using System;
using System.Collections.Generic;
using DgtCherub.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UciComms.Data;

namespace DgtCherubTest
{
    /// <summary>
    /// The score as the physical DGT clock shows it.
    /// </summary>
    /// <remarks>
    /// The clock holds whatever it is given until told to stop, so only a change is sent. It is a
    /// small display driven through a native call, so the text is short and the writes are spaced
    /// out.
    /// </remarks>
    [TestClass]
    public class KibitzerClockDisplayTests
    {
        private static UciEngineEval Evaluation(int centipawns, int mateIn)
        {
            return new UciEngineEval(centipawns, mateIn, "b1c3", 18, new List<string> { "info depth 18" });
        }

        [TestMethod(DisplayName = "A white advantage is shown with a plus")]
        public void WhiteAdvantageIsSigned()
        {
            Assert.AreEqual("+0.27", KibitzerClockDisplay.Format(Evaluation(27, 0)));
            Assert.AreEqual("+1.45", KibitzerClockDisplay.Format(Evaluation(145, 0)));
        }

        [TestMethod(DisplayName = "A black advantage is shown with a minus")]
        public void BlackAdvantageIsSigned()
        {
            Assert.AreEqual("-1.13", KibitzerClockDisplay.Format(Evaluation(-113, 0)));
        }

        [TestMethod(DisplayName = "An even position has no sign")]
        public void EvenPositionHasNoSign()
        {
            Assert.AreEqual("0.00", KibitzerClockDisplay.Format(Evaluation(0, 0)));
        }

        [TestMethod(DisplayName = "A mate is shown as a mate")]
        public void MateIsShownAsMate()
        {
            Assert.AreEqual("M3", KibitzerClockDisplay.Format(Evaluation(99970, 3)));
            Assert.AreEqual("M-2", KibitzerClockDisplay.Format(Evaluation(-99980, -2)));
        }

        [TestMethod(DisplayName = "The same score is not sent twice")]
        public void SameScoreIsNotResent()
        {
            KibitzerClockDisplay display = new();
            DateTime start = new(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);

            Assert.IsTrue(display.ShouldSend("+0.27", start), "the first score must be sent");
            Assert.IsFalse(display.ShouldSend("+0.27", start.AddSeconds(5)),
                           "the clock is holding it, so there is nothing to send");
        }

        [TestMethod(DisplayName = "Writes to the clock are spaced out")]
        public void WritesAreSpacedOut()
        {
            KibitzerClockDisplay display = new();
            DateTime start = new(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);

            Assert.IsTrue(display.ShouldSend("+0.27", start));
            Assert.IsFalse(display.ShouldSend("+0.30", start.AddMilliseconds(100)),
                           "an engine reports several times a second and the clock cannot keep up");
            Assert.IsTrue(display.ShouldSend("+0.30", start.AddSeconds(1)),
                          "once the interval has passed the new score goes out");
        }

        [TestMethod(DisplayName = "An empty score is never sent")]
        public void EmptyScoreIsNotSent()
        {
            KibitzerClockDisplay display = new();

            Assert.IsFalse(display.ShouldSend("", DateTime.UtcNow));
            Assert.IsFalse(display.ShouldSend(null!, DateTime.UtcNow));
        }

        [TestMethod(DisplayName = "Resetting lets the same score be sent again")]
        public void ResetAllowsResend()
        {
            KibitzerClockDisplay display = new();
            DateTime start = new(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);

            Assert.IsTrue(display.ShouldSend("+0.27", start));
            Assert.IsFalse(display.ShouldSend("+0.27", start.AddSeconds(5)));

            // Switching the kibitzer off gives the clock its own display back; switching it on
            // again has to put the score back even though it has not changed.
            display.Reset();

            Assert.IsTrue(display.ShouldSend("+0.27", start.AddSeconds(6)));
        }
    }
}
