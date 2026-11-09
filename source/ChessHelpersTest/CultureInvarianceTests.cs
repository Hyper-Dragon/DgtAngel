using ChessHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using UciComms;
using UciComms.Data;

namespace ChessHelpersTest
{
    /// <summary>
    /// The parsing and formatting that must mean the same thing on every machine.
    /// </summary>
    /// <remarks>
    /// Every case here is run once under the invariant culture and then again under cultures that
    /// disagree with it about something the code could accidentally depend on: de-DE uses a comma
    /// as the decimal separator and a dot for grouping, tr-TR changes the casing of i, fr-FR shares
    /// the de-DE decimal comma. A culture-sensitive parse in any of these paths shows up as a
    /// different answer under one of them, which is exactly the bug this suite is here to prevent.
    /// </remarks>
    [TestClass]
    public class CultureInvarianceTests
    {
        /// <summary>
        /// Cultures chosen for the ways they differ from invariant: decimal separator, digit
        /// grouping, and the dotted/dotless i.
        /// </summary>
        private static readonly string[] HostileCultures = { "de-DE", "tr-TR", "fr-FR" };

        private const string StartBoard = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";

        [TestMethod(DisplayName = "A FEN board field decodes the same under every culture")]
        public void FenDecodesIdenticallyAcrossCultures()
        {
            char[] invariant = WithCulture(CultureInfo.InvariantCulture, () => FenInference.FenToCharArray(StartBoard));

            foreach (char[] decoded in ForEachCulture(() => FenInference.FenToCharArray(StartBoard)))
            {
                CollectionAssert.AreEqual(invariant, decoded, "the same board must decode the same way regardless of locale");
            }

            Assert.AreEqual(64, invariant.Length);
            Assert.AreEqual('r', invariant[0], "a8 is the black rook");
            Assert.AreEqual('R', invariant[63], "h1 is the white rook");
        }

        [TestMethod(DisplayName = "A full FEN is reduced to its board field under every culture")]
        public void FullFenHandlingIsCultureInvariant()
        {
            const string fullFen = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1";

            char[] invariant = WithCulture(CultureInfo.InvariantCulture, () => FenInference.FenToCharArray(fullFen));

            foreach (char[] decoded in ForEachCulture(() => FenInference.FenToCharArray(fullFen)))
            {
                CollectionAssert.AreEqual(invariant, decoded, "the castling and clock fields must not leak into the board");
            }
        }

        [TestMethod(DisplayName = "Building a FEN from squares gives the same text under every culture")]
        public void FenBuildingIsCultureInvariant()
        {
            char[] squares = "".PadRight(64, '-').ToCharArray();
            squares[0] = 'r';
            squares[63] = 'R';

            string invariant = WithCulture(CultureInfo.InvariantCulture, () => squares.ConvertFlatArrayToFen());

            foreach (string built in ForEachCulture(() => squares.ConvertFlatArrayToFen()))
            {
                Assert.AreEqual(invariant, built, "the run lengths in a FEN must not be formatted by the current locale");
            }

            StringAssert.StartsWith(invariant, "r7/", "the empty run after the rook must be digits, not a localised number");
        }

        /// <summary>
        /// The driver reports its board as runs that can cross ranks; the normaliser has to turn
        /// that into a FEN board field. It picks the shapes apart by matching characters, so the
        /// matching must not be culture-sensitive.
        /// </summary>
        [TestMethod(DisplayName = "The DGT board normaliser behaves the same under every culture")]
        public void DgtBoardNormalisationIsCultureInvariant()
        {
            const string crossRank = "rnbqkbnrpppppppp32PPPPPPPP1NBQKBNR";

            string invariant = WithCulture(CultureInfo.InvariantCulture, () => crossRank.NormaliseDgtBoardToFen());

            foreach (string normalised in ForEachCulture(() => crossRank.NormaliseDgtBoardToFen()))
            {
                Assert.AreEqual(invariant, normalised);
            }

            StringAssert.Contains(invariant, "/", "a decoded board must come back as a real FEN board field");
        }

        /// <summary>
        /// The one with a number in it. An engine reporting fractional nodes must be read the same
        /// way everywhere: under a decimal-comma culture the point is a grouping separator, so a
        /// culture-sensitive parse turns 1234.5 into 12345.
        /// </summary>
        [TestMethod(DisplayName = "A fractional engine value parses the same under every culture")]
        public void UciNumbersParseInvariantly()
        {
            const string infoLine = "info depth 20 seldepth 30 time 1500 nodes 1234.5 nps 900000";

            decimal invariant = WithCulture(CultureInfo.InvariantCulture, () => ParseNodes(infoLine));

            Assert.AreEqual(1234.5m, invariant, "the reference parse must be the decimal 1234.5");

            foreach (decimal nodes in ForEachCulture(() => ParseNodes(infoLine)))
            {
                Assert.AreEqual(
                    1234.5m,
                    nodes,
                    "nodes must not be read with the current locale - a decimal comma culture would make this 12345");
            }
        }

        [TestMethod(DisplayName = "Engine lines are recognised by token, whatever the culture")]
        public void UciTokenMatchingIsCultureInvariant()
        {
            const string infoLine = "info depth 12 nodes 800 pv e2e4 e7e5";

            foreach (InfoResponse parsed in ForEachCulture(() => ParseInfo(infoLine)))
            {
                Assert.AreEqual(12, parsed.Depth);
                Assert.AreEqual("e2e4", parsed.Pv?.Split(' ')[0]);
            }
        }

        private static InfoResponse ParseInfo(string line)
        {
            UciChessEngine engine = new(new System.IO.FileInfo("engine-not-started.exe"));
            return (InfoResponse)engine.ParseResponse(line)!;
        }

        private static decimal ParseNodes(string line) => ParseInfo(line).Nodes;

        /// <summary>Runs the assertion under the invariant culture, then under each hostile one.</summary>
        private static IEnumerable<T> ForEachCulture<T>(Func<T> action)
        {
            foreach (string name in HostileCultures)
            {
                yield return WithCulture(new CultureInfo(name), action);
            }
        }

        private static T WithCulture<T>(CultureInfo culture, Func<T> action)
        {
            CultureInfo previousCulture = Thread.CurrentThread.CurrentCulture;
            CultureInfo previousUiCulture = Thread.CurrentThread.CurrentUICulture;

            try
            {
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;

                return action();
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previousCulture;
                Thread.CurrentThread.CurrentUICulture = previousUiCulture;
            }
        }
    }
}
