using DgtCherubTest.TestSupport;
using DgtLiveChessWrapper.DgtLiveChessJson;
using DgtRabbitWrapper;
using DgtRabbitWrapper.DgtEbDll;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace DgtCherubTest
{
    /// <summary>
    /// The LiveChess JSON records are a contract with a program this repository does not
    /// own. Field names come from DGT's published API, so a rename here silently breaks
    /// board detection.
    /// </summary>
    [TestClass]
    public class DgtLiveChessJsonTests
    {
        // Shape taken from a real LiveChess reply captured on this machine.
        private const string EBoardsJson = """
            {"response":"call","id":1,"param":[{"serialnr":"23451","source":"COM3","state":"ACTIVE","battery":"87%","comment":null,"board":"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR","flipped":false,"clock":null}],"time":1762000000000}
            """;

        private const string FeedJson = """
            {"response":"feed","id":1,"param":{"serialnr":"23451","flipped":false,"board":"rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR"},"time":1762000000000}
            """;

        [TestMethod(DisplayName = "An eboards reply is read")]
        public void EBoardsReply()
        {
            (string raw, LiveChessCallResponse response) = LiveChessCallResponse.Deserialize(EBoardsJson);

            Assert.AreEqual(EBoardsJson, raw, "the raw text is carried through for logging");
            Assert.AreEqual("call", response.Response);
            Assert.AreEqual(1, response.Id);
            Assert.AreEqual(1, response.Boards.Length);

            LiveChessCallParams board = response.Boards[0];
            Assert.AreEqual("23451", board.SerialNumber);
            Assert.AreEqual("COM3", board.Source);
            Assert.AreEqual("ACTIVE", board.ConnectionState, "board detection keys off this exact value");
            Assert.AreEqual("87%", board.BatteryLevel);
            Assert.IsFalse(board.Flipped);
        }

        [TestMethod(DisplayName = "An eboards reply with no boards yields an empty list")]
        public void EBoardsEmpty()
        {
            (_, LiveChessCallResponse response) = LiveChessCallResponse.Deserialize("""{"response":"call","id":1,"param":[],"time":1}""");

            Assert.IsNotNull(response.Boards);
            Assert.AreEqual(0, response.Boards.Length);
        }

        [TestMethod(DisplayName = "A feed reply is read")]
        public void FeedReply()
        {
            (_, LiveChessFeedResponse response) = LiveChessFeedResponse.Deserialize(FeedJson);

            Assert.AreEqual("feed", response.Response);
            Assert.IsNotNull(response.Param);
            Assert.AreEqual("23451", response.Param.Serialnr);
            Assert.AreEqual("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR", response.Param.Board);
            Assert.IsFalse(response.Param.Flipped);
        }

        [TestMethod(DisplayName = "A flipped board is reported as flipped")]
        public void FlippedBoard()
        {
            (_, LiveChessFeedResponse response) = LiveChessFeedResponse.Deserialize(
                """{"response":"feed","id":1,"param":{"serialnr":"1","flipped":true,"board":"8/8/8/8/8/8/8/8"}}""");

            Assert.IsTrue(response.Param.Flipped);
        }

        [TestMethod(DisplayName = "Malformed JSON is rejected rather than returning a half object")]
        public void MalformedJsonIsRejected()
        {
            _ = Assert.ThrowsExactly<JsonException>(() => LiveChessCallResponse.Deserialize("{ not json"));
        }
    }

    /// <summary>
    /// The transmission path of the fake LiveChess feed. Previously untestable because it
    /// took Fleck's connection type directly.
    /// </summary>
    [TestClass]
    public class LiveChessServerSocketTests
    {
        private sealed class FakeLiveChessSocket : ILiveChessSocket
        {
            private readonly bool _throwOnSend;

            public FakeLiveChessSocket(int clientPort, bool throwOnSend = false)
            {
                ClientPort = clientPort;
                _throwOnSend = throwOnSend;
            }

            public int ClientPort { get; }

            public List<string> Sent { get; } = new();

            public Task Send(string message)
            {
                if (_throwOnSend)
                {
                    throw new InvalidOperationException("synthetic send failure");
                }

                Sent.Add(message);
                return Task.CompletedTask;
            }
        }

        private static LiveChessServer CreateServer() =>
            new(new Mock<IDgtEbDllFacade>(MockBehavior.Loose).Object, 1234, 3, 100, "8/8/8/8/8/8/8/8");

        [TestMethod(DisplayName = "A message reaches the client")]
        public void MessageIsSent()
        {
            LiveChessServer server = CreateServer();
            FakeLiveChessSocket socket = new(clientPort: 5000);

            server.SendToSocket(socket, "hello");

            CollectionAssert.AreEqual(new[] { "hello" }, socket.Sent);
        }

        [TestMethod(DisplayName = "The outbound message is logged with the client port")]
        public void MessageIsLogged()
        {
            LiveChessServer server = CreateServer();
            FakeLiveChessSocket socket = new(clientPort: 5000);

            List<string> logged = new();
            server.OnLiveChessSrvMessage += (_, message) => logged.Add(message);

            server.SendToSocket(socket, "hello");

            Assert.AreEqual(1, logged.Count);
            StringAssert.Contains(logged[0], "OUT::5000::hello");
        }

        [TestMethod(DisplayName = "A failed send is reported and does not escape")]
        public void FailedSendIsReported()
        {
            LiveChessServer server = CreateServer();
            FakeLiveChessSocket socket = new(clientPort: 5000, throwOnSend: true);

            List<string> logged = new();
            server.OnLiveChessSrvMessage += (_, message) => logged.Add(message);

            server.SendToSocket(socket, "hello");

            Assert.IsTrue(logged.Exists(l => l.StartsWith("ERR::")), "a send failure must be reported");
        }
    }
}
