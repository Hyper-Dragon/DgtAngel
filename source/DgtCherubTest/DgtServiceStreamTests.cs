using DgtCherub.Services;
using DgtCherubTest.TestSupport;
using DgtGrpcService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DgtCherubTest
{
    /// <summary>
    /// Ordering and lifecycle of the gRPC callback streams.
    ///
    /// These are the streams a proxied client sees, and out-of-order delivery is exactly
    /// what made a proxied GUI show "piece lifted, never lands". The stream is fed by a
    /// bounded channel that drops the oldest entries under backpressure, so "which events
    /// survive" is as much a contract as "which order they arrive in".
    /// </summary>
    [TestClass]
    public class DgtServiceStreamTests
    {
        private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

        private static (DgtService service, FakeBoardEventSource board) CreateService()
        {
            FakeBoardEventSource board = new();
            Mock<IAngelHubService> hub = new(MockBehavior.Loose);

            return (new DgtService(board, hub.Object), board);
        }

        [TestMethod(DisplayName = "Driver events arrive in the order they were raised")]
        public async Task EventsArriveInOrder()
        {
            (DgtService service, FakeBoardEventSource board) = CreateService();

            using CancellationTokenSource cancellation = new();
            CollectingStreamWriter<StringResponse> writer = new();

            Task stream = service.RegisterStableBoardFunc(new Empty(), writer, TestServerCallContext.Create(cancellation.Token));

            for (int i = 0; i < 12; i++)
            {
                board.RaiseStableBoard($"position-{i}");
            }

            await writer.WaitForCountAsync(12, Patience);

            string[] expected = Enumerable.Range(0, 12).Select(i => $"position-{i}").ToArray();
            string[] actual = writer.Received.Select(r => r.Value).ToArray();

            CollectionAssert.AreEqual(expected, actual, "callbacks must be relayed in the order the driver fired them");

            cancellation.Cancel();
            await stream.WaitAsync(Patience);
        }

        [TestMethod(DisplayName = "Only the registered callback is relayed")]
        public async Task OnlyTheRequestedCallbackIsRelayed()
        {
            (DgtService service, FakeBoardEventSource board) = CreateService();

            using CancellationTokenSource cancellation = new();
            CollectingStreamWriter<StringResponse> writer = new();

            Task stream = service.RegisterStableBoardFunc(new Empty(), writer, TestServerCallContext.Create(cancellation.Token));

            board.RaiseStatus("not for this stream");
            board.RaiseStableBoard("for this stream");

            await writer.WaitForCountAsync(1, Patience);

            Assert.AreEqual(1, writer.Received.Count, "a status event must not appear on the stable-board stream");
            Assert.AreEqual("for this stream", writer.Received[0].Value);

            cancellation.Cancel();
            await stream.WaitAsync(Patience);
        }

        [TestMethod(DisplayName = "Two clients on the same callback both receive every event")]
        public async Task TwoClientsBothReceive()
        {
            (DgtService service, FakeBoardEventSource board) = CreateService();

            using CancellationTokenSource cancellation = new();
            CollectingStreamWriter<StringResponse> first = new();
            CollectingStreamWriter<StringResponse> second = new();

            Task firstStream = service.RegisterStableBoardFunc(new Empty(), first, TestServerCallContext.Create(cancellation.Token));
            Task secondStream = service.RegisterStableBoardFunc(new Empty(), second, TestServerCallContext.Create(cancellation.Token));

            for (int i = 0; i < 5; i++)
            {
                board.RaiseStableBoard($"pos-{i}");
            }

            await first.WaitForCountAsync(5, Patience);
            await second.WaitForCountAsync(5, Patience);

            CollectionAssert.AreEqual(
                first.Received.Select(r => r.Value).ToArray(),
                second.Received.Select(r => r.Value).ToArray(),
                "one slow client must not steal events from the other");

            cancellation.Cancel();
            await Task.WhenAll(firstStream, secondStream).WaitAsync(Patience);
        }

        [TestMethod(DisplayName = "Cancelling the call ends the stream and detaches the handler")]
        public async Task CancellingDetachesTheHandler()
        {
            (DgtService service, FakeBoardEventSource board) = CreateService();

            CancellationTokenSource cancellation = new();
            CollectingStreamWriter<StringResponse> writer = new();

            Task stream = service.RegisterStableBoardFunc(new Empty(), writer, TestServerCallContext.Create(cancellation.Token));

            board.RaiseStableBoard("before");
            await writer.WaitForCountAsync(1, Patience);

            Assert.AreEqual(1, board.StableBoardSubscriberCount, "the stream should be subscribed while running");

            cancellation.Cancel();
            await stream.WaitAsync(Patience);

            Assert.AreEqual(0, board.StableBoardSubscriberCount, "a cancelled stream must not leave a subscription behind");

            board.RaiseStableBoard("after");
            await Task.Delay(50);

            Assert.AreEqual(1, writer.Received.Count, "nothing should be delivered after cancellation");
        }

        /// <summary>
        /// The stream buffers 64 entries with DropOldest. A burst while the client is not
        /// reading must therefore keep the newest events and preserve their relative order -
        /// silently reordering them is what corrupts a client's view of the board.
        /// </summary>
        [TestMethod(DisplayName = "A burst behind a stalled client keeps the newest events in order")]
        public async Task BurstBehindStalledClientDropsOldest()
        {
            (DgtService service, FakeBoardEventSource board) = CreateService();

            using CancellationTokenSource cancellation = new();
            CollectingStreamWriter<StringResponse> writer = new(holdFirstWrite: true);

            Task stream = service.RegisterStableBoardFunc(new Empty(), writer, TestServerCallContext.Create(cancellation.Token));

            board.RaiseStableBoard("first");

            // Wait until the first event is actually in the writer, so the channel is
            // guaranteed to be consuming from the second event onwards.
            await writer.FirstWriteStarted.WaitAsync(Patience);

            for (int i = 1; i <= 199; i++)
            {
                board.RaiseStableBoard(i.ToString("D3"));
            }

            writer.ReleaseFirstWrite();

            // 1 in the writer plus the 64 the bounded channel retained.
            await writer.WaitForCountAsync(65, Patience);

            IReadOnlyList<StringResponse> received = writer.Received;

            Assert.AreEqual(65, received.Count, "the buffer must cap at 64 pending events");
            Assert.AreEqual("first", received[0].Value, "the event already being written must not be discarded");

            string[] survivors = received.Skip(1).Select(r => r.Value).ToArray();
            string[] expectedSurvivors = Enumerable.Range(136, 64).Select(i => i.ToString("D3")).ToArray();

            CollectionAssert.AreEqual(expectedSurvivors, survivors, "the newest events must survive, in order");

            cancellation.Cancel();
            await stream.WaitAsync(Patience);
        }

        [TestMethod(DisplayName = "A failed write ends the stream instead of spinning")]
        public async Task FailedWriteEndsTheStream()
        {
            (DgtService service, FakeBoardEventSource board) = CreateService();

            using CancellationTokenSource cancellation = new();
            FailingStreamWriter<StringResponse> writer = new();

            Task stream = service.RegisterStableBoardFunc(new Empty(), writer, TestServerCallContext.Create(cancellation.Token));

            board.RaiseStableBoard("boom");

            await stream.WaitAsync(Patience);

            Assert.AreEqual(1, writer.Attempts, "a dead connection must not be retried in a loop");
            Assert.AreEqual(0, board.StableBoardSubscriberCount, "the dead stream must detach its handler");
        }
    }
}
