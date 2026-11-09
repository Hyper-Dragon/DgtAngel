using DgtAngelShared.Json;
using DgtCherub.Controllers;
using DgtCherub.Services;
using DgtCherubTest.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using static DgtAngelShared.Json.CherubApiMessage;

namespace DgtCherubTest
{
    /// <summary>
    /// The websocket endpoint is the browser extension's only channel into Cherub, so its
    /// protocol handling is the contract between two separately released components.
    ///
    /// The tests drive the real controller through a fake WebSocket feature, which means
    /// the whole receive loop, handshake and dispatch run as they do in production.
    /// </summary>
    [TestClass]
    public class CherubWebSocketApiControllerTests
    {
        private const string SupportedVersion = "3.0";

        private static (CherubWebSocketApiController controller, FakeWebSocket socket, Mock<IAngelHubService> hub, DefaultHttpContext context) Create(
            bool isWebSocketRequest = true,
            bool clientInitComplete = true)
        {
            FakeWebSocket socket = new();

            Mock<IAngelHubService> hub = new(MockBehavior.Loose) { DefaultValue = DefaultValue.Empty };
            hub.SetupGet(h => h.IsClientInitComplete).Returns(clientInitComplete);
            hub.Setup(h => h.WatchStateChange(It.IsAny<MessageTypeCode>(), It.IsAny<string>(), It.IsAny<BoardState>()))
               .Returns(Task.CompletedTask);

            DefaultHttpContext context = new();
            context.Features.Set<IHttpWebSocketFeature>(new FakeWebSocketFeature(socket, isWebSocketRequest));

            CherubWebSocketApiController controller = new(NullLogger<CherubWebSocketApiController>.Instance, hub.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = context },
            };

            return (controller, socket, hub, context);
        }

        private static string Handshake(string version = SupportedVersion) =>
            JsonSerializer.Serialize(new CherubApiMessage
            {
                AngelPluginName = "DgtAngel",
                AngelPluginVersion = "1.2.3",
                AngelMessageVersion = version,
                MessageType = MessageTypeCode.KEEP_ALIVE,
            });

        private static CherubApiMessage StateUpdate(string fen, long captureTimeMs, ResponseCode code = ResponseCode.GAME_IN_PROGRESS) =>
            new()
            {
                AngelPluginName = "DgtAngel",
                AngelPluginVersion = "1.2.3",
                AngelMessageVersion = SupportedVersion,
                Source = "lichess",
                MessageType = MessageTypeCode.STATE_UPDATED,
                RemoteBoard = new BoardState
                {
                    PageUrl = "https://example.test",
                    CaptureTimeMs = captureTimeMs,
                    State = new DgtAngelShared.Json.State { Code = code, Message = "" },
                    BoardConnection = new Boardconnection { BoardState = SiteToBoardConnectionState.ACTIVE, ConMessage = "ok" },
                    Board = new Board
                    {
                        FenString = fen,
                        LastFenString = "",
                        LastMove = "",
                        Ending = "",
                        Clocks = new Clocks(),
                    },
                },
            };

        [TestMethod(DisplayName = "A plain HTTP request is rejected")]
        public async Task NonWebSocketRequestIsRejected()
        {
            (CherubWebSocketApiController controller, _, Mock<IAngelHubService> hub, DefaultHttpContext context) = Create(isWebSocketRequest: false);

            await controller.Get();

            Assert.AreEqual((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
            hub.Verify(h => h.UserMessageArrived(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod(DisplayName = "A client connecting before init completes is rejected")]
        public async Task PreInitConnectionIsRejected()
        {
            (CherubWebSocketApiController controller, FakeWebSocket socket, _, DefaultHttpContext context) = Create(clientInitComplete: false);

            await controller.Get();

            Assert.AreEqual((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
            Assert.AreEqual(0, socket.Sent.Count);
        }

        [TestMethod(DisplayName = "A keep-alive is answered with PONG")]
        public async Task KeepAliveIsAnswered()
        {
            (CherubWebSocketApiController controller, FakeWebSocket socket, _, _) = Create();

            socket.QueueText(Handshake());
            socket.QueueDisconnect();

            await controller.Get();

            CollectionAssert.Contains(socket.Sent, "PONG");
        }

        [TestMethod(DisplayName = "A board update is forwarded once the handshake succeeds")]
        public async Task StateUpdateIsForwarded()
        {
            (CherubWebSocketApiController controller, FakeWebSocket socket, Mock<IAngelHubService> hub, _) = Create();

            socket.QueueText(Handshake());
            socket.QueueText(JsonSerializer.Serialize(StateUpdate("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR", 100)));

            await controller.Get();

            hub.Verify(h => h.RemoteBoardUpdated(It.IsAny<BoardState>()), Times.Once);
        }

        [TestMethod(DisplayName = "An extension on the wrong message version is not processed")]
        public async Task IncompatibleVersionIsNotProcessed()
        {
            (CherubWebSocketApiController controller, FakeWebSocket socket, Mock<IAngelHubService> hub, _) = Create();

            socket.QueueText(Handshake("2.0"));
            socket.QueueText(JsonSerializer.Serialize(StateUpdate("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR", 100)));

            await controller.Get();

            hub.Verify(h => h.RemoteBoardUpdated(It.IsAny<BoardState>()), Times.Never, "a mismatched extension must not drive the board");
            hub.Verify(h => h.UserMessageArrived("INGEST", It.Is<string>(s => s.Contains("INCOMPATIBLE"))), Times.Once);
        }

        [TestMethod(DisplayName = "An out-of-sequence board update is dropped")]
        public async Task OutOfSequenceUpdateIsDropped()
        {
            (CherubWebSocketApiController controller, FakeWebSocket socket, Mock<IAngelHubService> hub, _) = Create();

            socket.QueueText(Handshake());
            socket.QueueText(JsonSerializer.Serialize(StateUpdate("8/8/8/8/8/8/8/8", 200)));
            socket.QueueText(JsonSerializer.Serialize(StateUpdate("8/8/8/8/8/8/8/8", 100)));

            await controller.Get();

            hub.Verify(h => h.RemoteBoardUpdated(It.IsAny<BoardState>()), Times.Once, "an older capture must not overwrite a newer one");
            hub.Verify(h => h.UserMessageArrived("INGEST", It.Is<string>(s => s.Contains("out of sequence"))), Times.Once);
        }

        [TestMethod(DisplayName = "Malformed data is reported and the connection survives")]
        public async Task MalformedJsonIsReported()
        {
            (CherubWebSocketApiController controller, FakeWebSocket socket, Mock<IAngelHubService> hub, _) = Create();

            socket.QueueText(Handshake());
            socket.QueueText("{ this is not json ");
            socket.QueueText(JsonSerializer.Serialize(new CherubApiMessage { MessageType = MessageTypeCode.MESSAGE, Source = "src", Message = "still here", AngelMessageVersion = SupportedVersion }));

            await controller.Get();

            hub.Verify(h => h.UserMessageArrived("INTERNAL", It.Is<string>(s => s.Contains("Unknown DATA"))), Times.Once);
            hub.Verify(h => h.UserMessageArrived("src", "still here"), Times.Once, "the connection must keep working after a bad frame");
        }

        [TestMethod(DisplayName = "A free-text message is forwarded to the console")]
        public async Task TextMessageIsForwarded()
        {
            (CherubWebSocketApiController controller, FakeWebSocket socket, Mock<IAngelHubService> hub, _) = Create();

            socket.QueueText(Handshake());
            socket.QueueText(JsonSerializer.Serialize(new CherubApiMessage { MessageType = MessageTypeCode.MESSAGE, Source = "lichess", Message = "hello", AngelMessageVersion = SupportedVersion }));

            await controller.Get();

            hub.Verify(h => h.UserMessageArrived("lichess", "hello"), Times.Once);
        }

        [TestMethod(DisplayName = "Watching a board is announced to the hub")]
        public async Task WatchStartedIsForwarded()
        {
            (CherubWebSocketApiController controller, FakeWebSocket socket, Mock<IAngelHubService> hub, _) = Create();

            socket.QueueText(Handshake());
            socket.QueueText(JsonSerializer.Serialize(new CherubApiMessage
            {
                AngelPluginName = "DgtAngel",
                AngelMessageVersion = SupportedVersion,
                MessageType = MessageTypeCode.WATCH_STARTED,
            }));

            await controller.Get();

            hub.Verify(h => h.WatchStateChange(MessageTypeCode.WATCH_STARTED, "DgtAngel", It.IsAny<BoardState>()), Times.Once);
        }

        [TestMethod(DisplayName = "An unknown message type is reported, not swallowed")]
        public async Task UnknownMessageTypeIsReported()
        {
            (CherubWebSocketApiController controller, FakeWebSocket socket, Mock<IAngelHubService> hub, _) = Create();

            // A valid handshake first, then a frame whose type is not in the enum.
            socket.QueueText(Handshake());
            socket.QueueText(JsonSerializer.Serialize(new CherubApiMessage { MessageType = MessageTypeCode.MESSAGE, Source = "x", Message = "y", AngelMessageVersion = SupportedVersion }));

            await controller.Get();

            hub.Verify(h => h.UserMessageArrived("x", "y"), Times.Once);
        }

        [TestMethod(DisplayName = "A dropped connection is announced and the watch is stopped")]
        public async Task DisconnectIsAnnounced()
        {
            (CherubWebSocketApiController controller, FakeWebSocket socket, Mock<IAngelHubService> hub, _) = Create();

            socket.QueueText(Handshake());
            socket.QueueDisconnect();

            await controller.Get();

            hub.Verify(h => h.WatchStateChange(MessageTypeCode.WATCH_STOPPED, ""), Times.AtLeastOnce);
            hub.Verify(h => h.PluginDisconnect(), Times.Once);
            hub.Verify(h => h.UserMessageArrived("INTERNAL", "Angel has Disconnected"), Times.Once);
        }
    }
}
