using DgtAngelShared.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;

namespace ChessHelpersTest
{
    /// <summary>
    /// The JSON types are the wire contract between the browser extension and Cherub.
    /// A change here breaks a component that is not in this repository, so these tests
    /// pin the shape rather than any particular implementation of it.
    /// </summary>
    [TestClass]
    public class DgtAngelSharedJsonTests
    {
        private static Board SampleBoard() => new()
        {
            IsWhiteOnBottom = true,
            ClockTurn = TurnCode.WHITE,
            FenTurn = TurnCode.BLACK,
            LastMove = "e4",
            Ending = "",
            FenString = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR",
            LastFenString = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR",
            Clocks = new Clocks { CaptureTimeMs = 1_700_000_000, WhiteClock = 300_000, BlackClock = 295_000 },
        };

        [TestMethod(DisplayName = "A message round-trips with every field intact")]
        public void MessageRoundTrips()
        {
            CherubApiMessage original = new()
            {
                AngelPluginName = "DgtAngel",
                AngelPluginVersion = "1.2.3",
                AngelMessageVersion = "3.0",
                MsgTimeMs = 1_700_000_001,
                Source = "lichess",
                Message = "hello",
                MessageType = CherubApiMessage.MessageTypeCode.STATE_UPDATED,
                RemoteBoard = new BoardState
                {
                    PageUrl = "https://lichess.org/abcd1234",
                    CaptureTimeMs = 1_700_000_002,
                    State = new State { Code = ResponseCode.GAME_IN_PROGRESS, Message = "playing" },
                    BoardConnection = new Boardconnection { BoardState = SiteToBoardConnectionState.ACTIVE, ConMessage = "ok" },
                    Board = SampleBoard(),
                },
            };

            string json = JsonSerializer.Serialize(original);
            CherubApiMessage? restored = JsonSerializer.Deserialize<CherubApiMessage>(json);

            Assert.IsNotNull(restored);
            Assert.AreEqual(original.AngelPluginName, restored.AngelPluginName);
            Assert.AreEqual(original.AngelMessageVersion, restored.AngelMessageVersion);
            Assert.AreEqual(original.MsgTimeMs, restored.MsgTimeMs);
            Assert.AreEqual(original.Source, restored.Source);
            Assert.AreEqual(original.MessageType, restored.MessageType);
            Assert.IsNotNull(restored.RemoteBoard);

            Assert.AreEqual(original.RemoteBoard.PageUrl, restored.RemoteBoard.PageUrl);
            Assert.AreEqual(original.RemoteBoard.State.Code, restored.RemoteBoard.State.Code);
            Assert.AreEqual(original.RemoteBoard.BoardConnection.BoardState, restored.RemoteBoard.BoardConnection.BoardState);

            Board board = restored.RemoteBoard.Board;
            Assert.AreEqual(true, board.IsWhiteOnBottom);
            Assert.AreEqual(TurnCode.WHITE, board.ClockTurn);
            Assert.AreEqual(TurnCode.BLACK, board.FenTurn);
            Assert.AreEqual("e4", board.LastMove);
            Assert.AreEqual(original.RemoteBoard.Board.FenString, board.FenString);
            Assert.AreEqual(300_000, board.Clocks.WhiteClock);
            Assert.AreEqual(295_000, board.Clocks.BlackClock);
        }

        /// <summary>
        /// Enums cross this boundary as names. A caller switching to numeric enums would
        /// still round-trip through this library, but the extension's parser would not.
        /// </summary>
        [TestMethod(DisplayName = "MessageType crosses the wire as a name")]
        public void MessageTypeIsAName()
        {
            CherubApiMessage message = new() { MessageType = CherubApiMessage.MessageTypeCode.WATCH_STARTED };

            string json = JsonSerializer.Serialize(message);

            StringAssert.Contains(json, "\"WATCH_STARTED\"");
        }

        [TestMethod(DisplayName = "Response codes cross the wire as names")]
        public void ResponseCodeIsAName()
        {
            State state = new() { Code = ResponseCode.GAME_PENDING };

            string json = JsonSerializer.Serialize(state);

            StringAssert.Contains(json, "\"GAME_PENDING\"");
        }

        /// <summary>
        /// ClockTurn and FenTurn are the same enum on the same record, so they must appear
        /// on the wire in the same form. One carries a JsonStringEnumConverter and one does
        /// not, which makes the same enum arrive as a name in one field and a number in the
        /// other - the extension has to special-case that, and nobody asked it to.
        /// </summary>
        [TestMethod(DisplayName = "ClockTurn and FenTurn cross the wire in the same form")]
        public void TurnFieldsAgreeOnWireFormat()
        {
            string json = JsonSerializer.Serialize(SampleBoard());

            StringAssert.Contains(json, "\"ClockTurn\":\"WHITE\"", "ClockTurn should be a name");
            StringAssert.Contains(json, "\"FenTurn\":\"BLACK\"", "FenTurn should be a name too");
        }

        [TestMethod(DisplayName = "A board state round-trips unchanged")]
        public void BoardStateRoundTrips()
        {
            BoardState original = new()
            {
                PageUrl = "https://www.chess.com/game/live/1",
                CaptureTimeMs = 42,
                State = new State { Code = ResponseCode.LOST_VISABILITY, Message = "gone" },
                BoardConnection = new Boardconnection { BoardState = SiteToBoardConnectionState.UNKNOWN, ConMessage = "no" },
                Board = SampleBoard(),
            };

            string json = JsonSerializer.Serialize(original);
            BoardState? restored = JsonSerializer.Deserialize<BoardState>(json);

            Assert.IsNotNull(restored);
            Assert.AreEqual(original.PageUrl, restored.PageUrl);
            Assert.AreEqual(ResponseCode.LOST_VISABILITY, restored.State.Code);
            Assert.AreEqual(SiteToBoardConnectionState.UNKNOWN, restored.BoardConnection.BoardState);
            Assert.AreEqual(original.Board.FenString, restored.Board.FenString);
        }

        [TestMethod(DisplayName = "Unknown message types fail to deserialise rather than defaulting")]
        public void UnknownMessageTypeFails()
        {
            const string json = "{\"MessageType\":\"NOT_A_REAL_CODE\"}";

            _ = Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<CherubApiMessage>(json));
        }
    }
}
