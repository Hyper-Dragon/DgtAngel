using System;
using UciComms.Data;

namespace DgtCherub.Helpers
{
    /// <summary>
    /// Turns an engine evaluation into the text and bar position the kibitzer panel shows.
    /// </summary>
    /// <remarks>
    /// The panel used to render only evaluations at depth 18 or beyond, and to write "Score: -"
    /// on every shallower one. That made the readout look broken for the whole of a fast game:
    /// an engine that answers in a fraction of a second never reaches depth 18, and the reset that
    /// precedes each new position was itself an evaluation of depth zero, so the labels were
    /// blanked continuously. Displaying what the engine reports, when it reports it, is both
    /// truthful and what a kibitzer is for.
    /// </remarks>
    internal static class KibitzerDisplay
    {
        internal const string NoScore = "Score: -";
        internal const string NoDepth = "Depth: -";

        /// <summary>
        /// Whether this evaluation is the reset that precedes a new position, rather than an
        /// evaluation of the current one.
        /// </summary>
        /// <remarks>
        /// A reset is recognised by having no analysed lines at all. Depth alone is not enough:
        /// depth zero is also a legitimate report from an engine that has only just started.
        /// </remarks>
        internal static bool IsReset(UciEngineEval engineEval)
        {
            return engineEval == null || engineEval.TopLines == null || engineEval.TopLines.Count == 0;
        }

        internal static string FormatScore(UciEngineEval engineEval)
        {
            if (engineEval.MateIn != 0)
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, "Mate : {0}", engineEval.MateIn);
            }

            // Centipawns are shown as pawns, which is what the readout has always done.
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "Score: {0}", engineEval.Eval / 100f);
        }

        internal static string FormatDepth(UciEngineEval engineEval)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "Depth: {0}", engineEval.Depth);
        }

        /// <summary>
        /// Where the evaluation bar should sit, as a value between zero and <paramref name="maximum"/>.
        /// </summary>
        /// <remarks>
        /// The score is clamped to plus or minus ten pawns before being centred, and the result is
        /// clamped to the bar's own range. Without the second clamp a bar configured with a maximum
        /// below twice the score range would throw rather than simply sit at the end.
        /// </remarks>
        internal static int ProgressValue(int centipawns, int maximum)
        {
            int centred = Math.Clamp(centipawns / 100, -10, 10) + (maximum / 2);
            return Math.Clamp(centred, Math.Min(0, maximum), Math.Max(0, maximum));
        }
    }
}
