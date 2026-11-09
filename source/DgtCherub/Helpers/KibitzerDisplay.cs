using System;
using System.Collections.Generic;
using System.Globalization;
using ChessHelpers;
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

        /// <summary>How many principal variations the panel lists, and how many moves of each.</summary>
        internal const int LINES_TO_SHOW = 3;
        internal const int MOVES_PER_LINE = 8;

        /// <summary>
        /// The information panel: what the position is, and what the engine makes of it.
        /// </summary>
        /// <remarks>
        /// The position facts come from the board rather than from the engine, because a DGT board
        /// reports only where the pieces are - the turn, the castling rights and en passant all
        /// have to be inferred. The lines are distinct principal variations: with a single-PV
        /// engine there is only one, and listing the same variation at three depths would be
        /// three copies of the same answer.
        /// </remarks>
        internal static string FormatInfo(KibitzerPosition position, UciEngineEval engineEval)
        {
            List<string> lines = new();

            if (position != null)
            {
                lines.Add(position.IsWhiteToPlay ? "White to play" : "Black to play");

                bool whiteCanCastle = position.WhiteCanCastleShort || position.WhiteCanCastleLong;
                bool blackCanCastle = position.BlackCanCastleShort || position.BlackCanCastleLong;
                lines.Add(string.Format(CultureInfo.InvariantCulture,
                    "Castling  White: {0}  Black: {1}",
                    whiteCanCastle ? "yes" : "no",
                    blackCanCastle ? "yes" : "no"));

                lines.Add(string.Format(CultureInfo.InvariantCulture,
                    "En passant  {0}",
                    position.EnPassantSquare == "-" ? "no" : position.EnPassantSquare));
            }

            List<string> variations = GetLines(position, engineEval);

            for (int line = 0; line < variations.Count; line++)
            {
                lines.Add(string.Format(CultureInfo.InvariantCulture, "{0}. {1}", line + 1, variations[line]));
            }

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// The engine's lines as readable notation, ready for the panel or a clock screen.
        /// </summary>
        /// <remarks>
        /// The engine speaks square to square, so each variation is replayed on the position it
        /// was calculated from and converted to algebraic notation. If the position is unknown, or
        /// the moves will not play, the raw move list is shown rather than nothing at all.
        /// </remarks>
        internal static List<string> GetLines(KibitzerPosition position, UciEngineEval engineEval)
        {
            List<string> lines = new();

            if (engineEval == null || IsReset(engineEval)) { return lines; }

            foreach (string variation in DistinctVariations(engineEval, LINES_TO_SHOW))
            {
                string notation = position == null
                    ? ""
                    : SanFormatter.ToNotation(position.Fen,
                                              variation.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                                              MOVES_PER_LINE);

                lines.Add(string.IsNullOrEmpty(notation) ? variation : notation);
            }

            return lines;
        }

        /// <summary>
        /// The engine's principal variations, deepest first, with repeats removed.
        /// </summary>
        /// <remarks>
        /// A single-PV engine reports the same variation at every depth, so the panel would
        /// otherwise list the same line three times. With MultiPV set, the variations differ and
        /// all three are shown.
        /// </remarks>
        private static List<string> DistinctVariations(UciEngineEval engineEval, int wanted)
        {
            List<string> variations = new();

            if (engineEval.TopLines == null) { return variations; }

            foreach (string infoLine in engineEval.TopLines)
            {
                string variation = VariationOf(infoLine);
                if (string.IsNullOrEmpty(variation) || variations.Contains(variation)) { continue; }

                variations.Add(variation);

                if (variations.Count == wanted) { break; }
            }

            return variations;
        }

        /// <summary>
        /// The move sequence of a UCI info line, or empty when it carries none.
        /// </summary>
        /// <remarks>
        /// The whole variation is returned, not a prefix: repeats are judged on the full line so
        /// that two variations differing only past the display limit are still distinct.
        /// </remarks>
        internal static string VariationOf(string infoLine)
        {
            if (string.IsNullOrWhiteSpace(infoLine)) { return ""; }

            string[] tokens = infoLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int pvIndex = Array.IndexOf(tokens, "pv");

            if (pvIndex < 0 || pvIndex + 1 >= tokens.Length) { return ""; }

            return string.Join(' ', tokens[(pvIndex + 1)..]);
        }
    }
}
