using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using ChessHelpers;
using DgtCherub.Helpers;
using UciComms.Data;

namespace DgtCherubTest
{
    /// <summary>
    /// What the kibitzer panel shows for a given engine evaluation.
    /// </summary>
    /// <remarks>
    /// The panel used to show nothing unless the engine reached depth eighteen, and blank itself
    /// for everything else - including for the reset that precedes each new position. On a board
    /// being played on, that is the whole session. These tests pin the replacement behaviour:
    /// whatever the engine reports is shown, and only a genuine reset clears the panel.
    /// </remarks>
    [TestClass]
    public class KibitzerDisplayTests
    {
        private static UciEngineEval Evaluation(int centipawns, int mateIn, int depth)
        {
            return new UciEngineEval(centipawns, mateIn, "b1c3", depth, new List<string> { "info depth ..." });
        }

        [TestMethod(DisplayName = "A shallow evaluation is still shown")]
        public void ShallowEvaluationIsShown()
        {
            UciEngineEval engineEval = Evaluation(27, 0, depth: 3);

            // The exact regression: at depth 3 the old panel wrote "Score: -".
            Assert.AreEqual("Score: 0.27", KibitzerDisplay.FormatScore(engineEval),
                            "an evaluation at any depth must be displayed, not withheld");
            Assert.AreEqual("Depth: 3", KibitzerDisplay.FormatDepth(engineEval));
            Assert.IsFalse(KibitzerDisplay.IsReset(engineEval), "a real evaluation is not a reset");
        }

        [TestMethod(DisplayName = "Centipawns are shown as pawns")]
        public void CentipawnsAreShownAsPawns()
        {
            Assert.AreEqual("Score: 0.27", KibitzerDisplay.FormatScore(Evaluation(27, 0, 6)));
            Assert.AreEqual("Score: -1.5", KibitzerDisplay.FormatScore(Evaluation(-150, 0, 6)));
            Assert.AreEqual("Score: 0", KibitzerDisplay.FormatScore(Evaluation(0, 0, 6)));
        }

        [TestMethod(DisplayName = "A mate is shown as a mate, not as a score")]
        public void MateIsShownAsMate()
        {
            Assert.AreEqual("Mate : 3", KibitzerDisplay.FormatScore(Evaluation(99970, 3, 12)));
            Assert.AreEqual("Mate : -2", KibitzerDisplay.FormatScore(Evaluation(-99980, -2, 12)));
        }

        [TestMethod(DisplayName = "A reset clears the panel")]
        public void ResetClearsThePanel()
        {
            UciEngineEval reset = new(0, 0, "", 0, new List<string>());

            Assert.IsTrue(KibitzerDisplay.IsReset(reset),
                          "the evaluation that precedes a new position has no analysed lines");
        }

        [TestMethod(DisplayName = "The progress bar sits in the middle for an even game")]
        public void ProgressBarIsCentred()
        {
            Assert.AreEqual(10, KibitzerDisplay.ProgressValue(0, 20));
        }

        [TestMethod(DisplayName = "The progress bar stays inside its range for a winning score")]
        public void ProgressBarClampsHigh()
        {
            Assert.AreEqual(20, KibitzerDisplay.ProgressValue(5000, 20));
        }

        [TestMethod(DisplayName = "The progress bar stays inside its range for a losing score")]
        public void ProgressBarClampsLow()
        {
            Assert.AreEqual(0, KibitzerDisplay.ProgressValue(-5000, 20));
        }

        [TestMethod(DisplayName = "The progress bar cannot exceed a maximum narrower than the score range")]
        public void ProgressBarClampsToASmallMaximum()
        {
            // The bar is 20 wide today, but the centipawn range spans 20 either side of centre.
            // A maximum below that would otherwise be handed a value it must reject.
            int value = KibitzerDisplay.ProgressValue(5000, 5);

            Assert.AreEqual(5, value, "the value must be clamped to the bar, not handed to it out of range");
        }

        // ---------------------------------------------------------------------------------
        // The information panel
        // ---------------------------------------------------------------------------------

        private const string InitialBoard = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";
        private const string AfterE4 = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR";

        private static string Info(UciEngineEval engineEval)
        {
            return KibitzerDisplay.FormatInfo(KibitzerPosition.Infer(AfterE4, InitialBoard), engineEval);
        }

        private static UciEngineEval WithLines(int depth, params string[] infoLines)
        {
            return new UciEngineEval(43, 0, "f3e5", depth, new List<string>(infoLines));
        }

        [TestMethod(DisplayName = "The panel says whose turn it is, the castling rights and en passant")]
        public void PanelShowsThePositionFacts()
        {
            string info = KibitzerDisplay.FormatInfo(
                KibitzerPosition.Infer(InitialBoard, ""),
                null);

            string[] lines = info.Split(Environment.NewLine);

            Assert.AreEqual("White to play", lines[0], "nothing has moved, so white starts");
            Assert.AreEqual("Castling  White: yes  Black: yes", lines[1]);
            Assert.AreEqual("En passant  no", lines[2]);
        }

        [TestMethod(DisplayName = "The panel names the en passant square when there is one")]
        public void PanelShowsTheEnPassantSquare()
        {
            string info = KibitzerDisplay.FormatInfo(
                KibitzerPosition.Infer(AfterE4, InitialBoard),
                null);

            StringAssert.Contains(info, "Black to play", "white has just moved");
            StringAssert.Contains(info, "En passant  e3", "the pawn crossed e3");
        }

        [TestMethod(DisplayName = "The panel lists the engine's variation")]
        public void PanelListsTheVariation()
        {
            string info = Info(WithLines(20,
                "info depth 20 score cp 43 time 100 nodes 5000 nps 50000 pv f3e5 d8e7 e5f7 e7e4"));

            StringAssert.Contains(info, "1. f3e5 d8e7 e5f7 e7e4");
        }

        [TestMethod(DisplayName = "The same variation at several depths is listed once")]
        public void RepeatedVariationIsListedOnce()
        {
            string info = Info(WithLines(20,
                "info depth 20 score cp 43 time 100 nodes 5000 nps 50000 pv f3e5 d8e7 e5f7",
                "info depth 19 score cp 41 time 90 nodes 4000 nps 44000 pv f3e5 d8e7 e5f7",
                "info depth 18 score cp 40 time 80 nodes 3000 nps 37000 pv f3e5 d8e7 e5f7"));

            StringAssert.Contains(info, "1. f3e5 d8e7 e5f7");

            // A single-PV engine reports one line at every depth; showing it three times would
            // be three copies of the same answer rather than three lines.
            Assert.IsFalse(info.Contains("2. f3e5", StringComparison.Ordinal),
                           $"a repeated variation must not be listed twice, got:{Environment.NewLine}{info}");
        }

        [TestMethod(DisplayName = "Three distinct variations are listed, deepest first")]
        public void DistinctVariationsAreListed()
        {
            string info = Info(WithLines(20,
                "info depth 20 multipv 1 score cp 43 pv f3e5 d8e7",
                "info depth 20 multipv 2 score cp 20 pv d2d4 d7d5",
                "info depth 20 multipv 3 score cp 12 pv c2c4 e7e5",
                "info depth 20 multipv 4 score cp 5 pv b1c3 b8c6"));

            StringAssert.Contains(info, "1. f3e5 d8e7");
            StringAssert.Contains(info, "2. d2d4 d7d5");
            StringAssert.Contains(info, "3. c2c4 e7e5");
            Assert.IsFalse(info.Contains("4. b1c3", StringComparison.Ordinal),
                           "only the top three lines are wanted");
        }

        [TestMethod(DisplayName = "A line is cut off after a readable number of moves")]
        public void VariationIsShortened()
        {
            string variation = KibitzerDisplay.VariationOf(
                "info depth 30 score cp 5 pv a2a3 b7b6 c2c3 d7d6 e2e3 f7f6 g2g3 h7h6 i2i3 j7j6");

            Assert.AreEqual("a2a3 b7b6 c2c3 d7d6 e2e3 f7f6 g2g3 h7h6", variation);
        }

        [TestMethod(DisplayName = "A line with no principal variation yields nothing")]
        public void LineWithoutPvYieldsNothing()
        {
            Assert.AreEqual("", KibitzerDisplay.VariationOf("info depth 5 time 17 nodes 7149 nps 430622"));
            Assert.AreEqual("", KibitzerDisplay.VariationOf("info currmove d2d3 currmovenumber 10"));
            Assert.AreEqual("", KibitzerDisplay.VariationOf(""));
        }

        [TestMethod(DisplayName = "With no engine the panel still describes the position")]
        public void PanelWorksWithoutAnEngine()
        {
            string info = Info(null!);

            StringAssert.Contains(info, "Black to play");
            Assert.IsFalse(info.Contains("1. ", StringComparison.Ordinal), "there is no variation to show");
        }
    }
}
