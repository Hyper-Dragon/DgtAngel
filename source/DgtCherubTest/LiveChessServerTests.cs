using DgtRabbitWrapper;
using DgtRabbitWrapper.DgtEbDll;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Text.Json;

namespace DgtCherubTest
{
    /// <summary>
    /// The fake LiveChess feed is consumed by third-party software, so its message shape is
    /// a wire contract. These tests pin the placeholder substitution rather than starting a
    /// websocket, which would need a real port and a real client.
    /// </summary>
    [TestClass]
    public class LiveChessServerTests
    {
        private static LiveChessServer CreateServer() =>
            new(new Mock<IDgtEbDllFacade>(MockBehavior.Loose).Object,
                boardSerialNo: 1234,
                comPort: 3,
                batteryPct: 100,
                initialFEN: "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR");

        [TestMethod(DisplayName = "FEN, serial and time placeholders are all substituted")]
        public void PlaceholdersAreSubstituted()
        {
            LiveChessServer server = CreateServer();

            string formatted = server.FormatMessage(
                "{\"board\":\"FENFEN\",\"serial\":\"BOARDNO\",\"time\":TIMETIME}",
                "8/8/8/8/8/8/8/8");

            Assert.IsFalse(formatted.Contains("FENFEN"), "no FEN placeholder may survive");
            Assert.IsFalse(formatted.Contains("BOARDNO"), "no serial placeholder may survive");
            Assert.IsFalse(formatted.Contains("TIMETIME"), "no time placeholder may survive");
        }

        [TestMethod(DisplayName = "The formatted message is valid JSON carrying the board")]
        public void FormattedMessageIsJson()
        {
            LiveChessServer server = CreateServer();
            const string board = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR";

            string formatted = server.FormatMessage("{\"board\":\"FENFEN\",\"time\":TIMETIME}", board);

            using JsonDocument document = JsonDocument.Parse(formatted);

            Assert.AreEqual(board, document.RootElement.GetProperty("board").GetString());
        }

        [TestMethod(DisplayName = "The timestamp is Unix milliseconds")]
        public void TimestampIsUnixMilliseconds()
        {
            LiveChessServer server = CreateServer();

            string formatted = server.FormatMessage("{\"time\":TIMETIME}");

            using JsonDocument document = JsonDocument.Parse(formatted);
            long timestamp = document.RootElement.GetProperty("time").GetInt64();

            // Between 2020 and 2100 - a plausible Unix millisecond stamp, not seconds or ticks.
            Assert.IsTrue(timestamp > 1_577_836_800_000, $"timestamp {timestamp} is too small for Unix milliseconds");
            Assert.IsTrue(timestamp < 4_102_444_800_000, $"timestamp {timestamp} is too large for Unix milliseconds");
        }

        [TestMethod(DisplayName = "The same message formats identically apart from the timestamp")]
        public void SerialIsStableAcrossCalls()
        {
            LiveChessServer server = CreateServer();

            string first = server.FormatMessage("{\"serial\":\"BOARDNO\"}");
            string second = server.FormatMessage("{\"serial\":\"BOARDNO\"}");

            Assert.AreEqual(first, second, "the board serial must not change between messages");
        }

        [TestMethod(DisplayName = "Text that is not a placeholder is left alone")]
        public void NonPlaceholdersAreLeftAlone()
        {
            LiveChessServer server = CreateServer();

            string formatted = server.FormatMessage("{\"state\":\"ACTIVE\",\"battery\":\"100%\"}");

            StringAssert.Contains(formatted, "ACTIVE");
            StringAssert.Contains(formatted, "100%");
        }
    }
}
