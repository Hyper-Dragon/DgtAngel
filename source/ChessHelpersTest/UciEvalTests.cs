using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;
using UciComms;
using UciComms.Data;

namespace ChessHelpersTest
{
    /// <summary>
    /// The kibitzer eval pipeline: engine output line in, <see cref="UciEngineEval"/> out.
    /// </summary>
    /// <remarks>
    /// Every line here was captured from Rybka 2.3.2a mp, so the formats are real rather than
    /// invented. Two shapes matter and are easy to get wrong:
    ///
    ///  - "info depth N" with no score - Rybka emits these between scored lines, and they must be
    ///    ignored rather than treated as an eval of zero.
    ///  - "info currmove X currmovenumber N" - no depth, no score, very chatty in the middlegame.
    ///
    /// These tests exist because the form showed no eval at all while the engine was demonstrably
    /// answering, and the pump is the only part of that path that can fail without a trace.
    /// </remarks>
    [TestClass]
    public class UciEvalTests
    {
        // Captured verbatim from Rybka 2.3.2a mp.
        private const string RybkaScoredLine =
            "info depth 6 score cp 27 hashfull 0 time 17 nodes 9627 nps 579885 pv b1c3 b8c6 g1f3";
        private const string RybkaDepthOnlyLine =
            "info depth 5 time 17 nodes 7149 nps 430622 ";
        private const string RybkaCurrMoveLine =
            "info currmove d2d3 currmovenumber 10";
        private const string RybkaMateLine =
            "info depth 12 score mate 3 time 100 nodes 2000 nps 20000 pv d1h5 g8h8 d5f7";

        private static UciChessEngine NewEngine()
        {
            // The constructor does not touch the file; nothing here starts a process.
            return new UciChessEngine(new FileInfo("not-a-real-engine.exe"));
        }

        [TestMethod(DisplayName = "A scored info line yields the centipawn score, depth and best move")]
        public void ScoredLineIsParsed()
        {
            (int? cp, int mateIn) = UciComms.Data.Eval.GetBoardEvalFromLine(RybkaScoredLine);

            Assert.AreEqual(27, cp, "the score is the number two tokens after 'score'");
            Assert.AreEqual(0, mateIn, "a cp score is not a mate score");
            Assert.AreEqual(6, UciComms.Data.Eval.GetDepthFromLine(RybkaScoredLine));
            Assert.AreEqual("b1c3", UciComms.Data.Eval.GetBestMoveFromLine(RybkaScoredLine));
        }

        [TestMethod(DisplayName = "A depth-only line reports a depth but no score")]
        public void DepthOnlyLineHasNoScore()
        {
            (int? cp, _) = UciComms.Data.Eval.GetBoardEvalFromLine(RybkaDepthOnlyLine);

            Assert.IsNull(cp, "a line with no score must not be read as an eval of zero");
            Assert.AreEqual(5, UciComms.Data.Eval.GetDepthFromLine(RybkaDepthOnlyLine));
        }

        [TestMethod(DisplayName = "A currmove line reports neither depth nor score")]
        public void CurrMoveLineHasNeither()
        {
            (int? cp, _) = UciComms.Data.Eval.GetBoardEvalFromLine(RybkaCurrMoveLine);

            Assert.IsNull(cp, "currmove lines carry no eval");
            Assert.IsNull(UciComms.Data.Eval.GetDepthFromLine(RybkaCurrMoveLine));
            Assert.IsNull(UciComms.Data.Eval.GetBestMoveFromLine(RybkaCurrMoveLine),
                          "'currmove' must not be mistaken for a pv move");
        }

        [TestMethod(DisplayName = "A mate score is converted to a very large centipawn value")]
        public void MateLineIsParsed()
        {
            (int? cp, int mateIn) = UciComms.Data.Eval.GetBoardEvalFromLine(RybkaMateLine);

            Assert.AreEqual(3, mateIn, "mate in 3 must be reported as such");
            Assert.IsTrue(cp > 99000, $"a winning mate must score near the top of the range, was {cp}");
        }

        [TestMethod(DisplayName = "Scored lines fed through the engine raise a board eval")]
        public void ScoredLinesReachTheEvalEvent()
        {
            UciChessEngine engine = NewEngine();
            using ManualResetEventSlim raised = new(false);

            UciEngineEval? last = null;
            engine.OnBoardEvalChanged += e => { last = e; raised.Set(); };

            engine.ParseResponse(RybkaScoredLine);

            Assert.IsTrue(raised.Wait(TimeSpan.FromSeconds(10)),
                          "a scored info line must raise OnBoardEvalChanged; no event arrived in 10s");

            Assert.IsNotNull(last);
            Assert.AreEqual(27, last.Eval, "the eval must carry the score from the line");
            Assert.AreEqual(6, last.Depth, "the eval must carry the depth from the line");
            Assert.AreEqual("b1c3", last.BestMove, "the eval must carry the pv move from the line");
        }

        [TestMethod(DisplayName = "Chatty unscored lines do not stop the eval pump")]
        public void UnscoredLinesDoNotKillThePump()
        {
            UciChessEngine engine = NewEngine();
            using ManualResetEventSlim raised = new(false);

            int count = 0;
            engine.OnBoardEvalChanged += _ => { count++; raised.Set(); };

            // A realistic burst: Rybka emits these between every scored line.
            engine.ParseResponse(RybkaDepthOnlyLine);
            engine.ParseResponse(RybkaCurrMoveLine);
            engine.ParseResponse(RybkaDepthOnlyLine);
            engine.ParseResponse(RybkaCurrMoveLine);
            engine.ParseResponse(RybkaScoredLine);
            engine.ParseResponse(RybkaCurrMoveLine);
            engine.ParseResponse(RybkaDepthOnlyLine);

            Assert.IsTrue(raised.Wait(TimeSpan.FromSeconds(10)),
                          "the scored line in the middle of a burst of noise must still raise an eval");
            Assert.IsTrue(count > 0, "the pump must survive the unscored lines that precede the scored one");
        }

        [TestMethod(DisplayName = "The reported depth is the highest seen, not the most recent")]
        public void ReportedDepthIsTheHighest()
        {
            UciChessEngine engine = NewEngine();
            using ManualResetEventSlim reachedDepth8 = new(false);

            UciEngineEval? last = null;
            engine.OnBoardEvalChanged += e =>
            {
                last = e;
                if (e.Depth >= 8) { reachedDepth8.Set(); }
            };

            engine.ParseResponse("info depth 8 score cp 15 time 10 nodes 100 nps 1000 pv e2e4");
            engine.ParseResponse("info depth 6 score cp 12 time 10 nodes 100 nps 1000 pv d2d4");

            Assert.IsTrue(reachedDepth8.Wait(TimeSpan.FromSeconds(10)), "the depth 8 line must be seen");
            Assert.IsNotNull(last);
            Assert.IsTrue(last.Depth >= 8,
                          $"a shallower line arriving later must not lower the reported depth, was {last.Depth}");
        }
    }
}
