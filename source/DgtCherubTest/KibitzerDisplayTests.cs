using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
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
    }
}
