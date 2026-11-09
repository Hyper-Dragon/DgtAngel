using System;
using System.Globalization;
using UciComms.Data;

namespace DgtCherub.Helpers
{
    /// <summary>
    /// What the physical clock is told to show, and how often.
    /// </summary>
    /// <remarks>
    /// The DGT clock holds a message on its display until it is told to stop, so the score stays
    /// up on its own. Only a change needs sending: re-sending the same text would drive the
    /// display for no reason, and an engine reports several times a second, so a minimum interval
    /// keeps the native calls down to something a clock can keep up with.
    /// </remarks>
    internal sealed class KibitzerClockDisplay
    {
        /// <summary>Shortest gap between two writes to the clock.</summary>
        internal static readonly TimeSpan MinimumInterval = TimeSpan.FromMilliseconds(750);

        private string lastText = "";
        private DateTime lastSentUtc = DateTime.MinValue;

        /// <summary>
        /// The score in as few characters as the clock can show: "+0.27", "-1.13", "M3", "M-2".
        /// </summary>
        internal static string Format(UciEngineEval engineEval)
        {
            if (engineEval is null) { return ""; }

            if (engineEval.MateIn != 0)
            {
                // A mate is not a number of pawns, and its sign already says who is mating.
                return string.Format(CultureInfo.InvariantCulture, "M{0}", engineEval.MateIn);
            }

            float pawns = engineEval.Eval / 100f;

            return string.Format(CultureInfo.InvariantCulture, "{0}{1:0.00}", pawns > 0 ? "+" : "", pawns);
        }

        /// <summary>
        /// Whether this text is worth sending now, recording it as sent when it is.
        /// </summary>
        internal bool ShouldSend(string text, DateTime nowUtc)
        {
            if (string.IsNullOrEmpty(text)) { return false; }

            // Already on the display - the clock is holding it.
            if (string.Equals(text, lastText, StringComparison.Ordinal)) { return false; }

            if (nowUtc - lastSentUtc < MinimumInterval) { return false; }

            lastText = text;
            lastSentUtc = nowUtc;

            return true;
        }

        /// <summary>
        /// Forgets what was last shown, so the next score is sent even if it is the same one.
        /// </summary>
        internal void Reset()
        {
            lastText = "";
            lastSentUtc = DateTime.MinValue;
        }
    }
}
