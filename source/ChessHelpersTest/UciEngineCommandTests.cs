using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using UciComms;

namespace ChessHelpersTest
{
    /// <summary>
    /// The commands the engine is actually sent, checked against the UCI specification.
    /// </summary>
    /// <remarks>
    /// These need no engine process. <c>SendCommand</c> raises <c>OnInputSentRaw</c> before it
    /// touches the process, so an engine with no process attached records exactly what would have
    /// gone down the pipe - which is the thing worth pinning, because the protocol mistakes here
    /// are all invisible: the engine accepts the bytes and quietly does the wrong thing.
    /// </remarks>
    [TestClass]
    public class UciEngineCommandTests
    {
        private const string StartFen = "rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq - 0 1";

        private static (UciChessEngine Engine, List<string> Sent) NewEngine()
        {
            // The constructor does not open the file and nothing here starts a process.
            UciChessEngine engine = new(new FileInfo("not-a-real-engine.exe"));

            List<string> sent = new();
            engine.OnInputSentRaw += (_, command) =>
            {
                if (!string.IsNullOrEmpty(command)) { sent.Add(command); }
            };

            return (engine, sent);
        }

        [TestMethod(DisplayName = "Setting a position sends the position and nothing else")]
        public void SetPositionSendsOnlyThePosition()
        {
            (UciChessEngine engine, List<string> sent) = NewEngine();

            engine.SetPosition(StartFen);

            Assert.AreEqual(1, sent.Count, $"one command must go out, got [{string.Join(" | ", sent)}]");
            Assert.AreEqual($"position fen {StartFen}", sent[0]);
        }

        [TestMethod(DisplayName = "Setting a position does not start a new game")]
        public void SetPositionDoesNotSendNewGame()
        {
            (UciChessEngine engine, List<string> sent) = NewEngine();

            engine.SetPosition(StartFen);

            // ucinewgame clears the engine's hash tables. Sending it per position means every ply
            // re-analyses from nothing, which is what a kibitzer on a live board cannot afford.
            CollectionAssert.DoesNotContain(sent, "ucinewgame",
                "ucinewgame belongs once per game, not once per position");
        }

        [TestMethod(DisplayName = "New game sends ucinewgame and nothing else")]
        public void NewGameSendsUciNewGame()
        {
            (UciChessEngine engine, List<string> sent) = NewEngine();

            engine.NewGame();

            Assert.AreEqual(1, sent.Count, $"one command must go out, got [{string.Join(" | ", sent)}]");
            Assert.AreEqual("ucinewgame", sent[0]);
        }

        [TestMethod(DisplayName = "Going infinite sends the UCI go command")]
        public void GoInfiniteSendsGoInfinite()
        {
            (UciChessEngine engine, List<string> sent) = NewEngine();

            engine.GoInfinite();

            Assert.AreEqual(1, sent.Count);
            Assert.AreEqual("go infinite", sent[0]);
        }

        [TestMethod(DisplayName = "Going to a depth sends the UCI go depth command")]
        public void GoSendsGoDepth()
        {
            (UciChessEngine engine, List<string> sent) = NewEngine();

            engine.Go(12);

            Assert.AreEqual(1, sent.Count);
            Assert.AreEqual("go depth 12", sent[0]);
        }

        [TestMethod(DisplayName = "A new game cycle is ucinewgame once then position per move")]
        public void AWholeGameEmitsTheSpecSequence()
        {
            (UciChessEngine engine, List<string> sent) = NewEngine();

            engine.NewGame();
            engine.SetPosition("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
            engine.SetPosition(StartFen);
            engine.SetPosition("rnbqkbnr/pppp1ppp/8/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R b KQkq - 0 1");

            Assert.AreEqual(4, sent.Count, $"expected one newgame and three positions, got [{string.Join(" | ", sent)}]");
            Assert.AreEqual("ucinewgame", sent[0], "the game starts with ucinewgame");
            Assert.IsFalse(sent.GetRange(1, 3).Contains("ucinewgame"),
                           "the positions within one game must not repeat ucinewgame");
        }
    }
}
