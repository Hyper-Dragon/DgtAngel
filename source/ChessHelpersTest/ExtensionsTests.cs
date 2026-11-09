using ChessHelpers;
using ChessHelpersTest.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace ChessHelpersTest
{
    [TestClass]
    public class ConvertFlatArrayToFenTests
    {
        [TestMethod(DisplayName = "Round-trips the start position")]
        public void RoundTripsStartPosition()
        {
            Assert.AreEqual(FenBoard.Start, FenBoard.Parse(FenBoard.Start).ConvertFlatArrayToFen());
        }

        [TestMethod(DisplayName = "Round-trips the empty board")]
        public void RoundTripsEmptyBoard()
        {
            Assert.AreEqual(FenBoard.Empty, FenBoard.Parse(FenBoard.Empty).ConvertFlatArrayToFen());
        }

        [TestMethod(DisplayName = "Round-trips boards produced by the independent parser")]
        public void RoundTripsSyntheticBoards()
        {
            const int boards = 5000;

            for (int seed = 1; seed <= boards; seed++)
            {
                string boardFen = FenBoard.Format(SyntheticBoards.RandomBoard(seed, seed % 30));

                Assert.AreEqual(boardFen, FenBoard.Parse(boardFen).ConvertFlatArrayToFen(), $"seed {seed}");
            }
        }

        [TestMethod(DisplayName = "A single piece is placed on the right square")]
        [DataRow("a8", 'K', "K7/8/8/8/8/8/8/8")]
        [DataRow("e4", 'Q', "8/8/8/8/4Q3/8/8/8")]
        [DataRow("a1", 'R', "8/8/8/8/8/8/8/R7")]
        [DataRow("h1", 'k', "8/8/8/8/8/8/8/7k")]
        public void SinglePiecePlacement(string square, char piece, string expected)
        {
            char[] flat = new char[64];
            Array.Fill(flat, '-');
            flat[FenBoard.Index(square)] = piece;

            Assert.AreEqual(expected, flat.ConvertFlatArrayToFen());
        }

        [TestMethod(DisplayName = "The tail is appended verbatim")]
        public void TailIsAppended()
        {
            string result = FenBoard.Parse(FenBoard.Empty).ConvertFlatArrayToFen(" w - - 0 1");

            Assert.AreEqual($"{FenBoard.Empty} w - - 0 1", result);
        }

        [TestMethod(DisplayName = "The output always contains exactly eight ranks")]
        public void OutputAlwaysHasEightRanks()
        {
            for (int seed = 1; seed <= 200; seed++)
            {
                string fen = FenBoard.Parse(FenBoard.Format(SyntheticBoards.RandomBoard(seed, seed % 30))).ConvertFlatArrayToFen();

                Assert.AreEqual(8, fen.Count(c => c == '/') + 1, $"seed {seed}: '{fen}'");
                Assert.AreEqual(64, FenBoard.Parse(fen).Length, $"seed {seed}: '{fen}'");
            }
        }
    }

    [TestClass]
    public class NormaliseDgtBoardToFenTests
    {
        [TestMethod(DisplayName = "Null is returned unchanged")]
        public void NullIsUnchanged()
        {
            Assert.IsNull(((string?)null)!.NormaliseDgtBoardToFen());
        }

        [TestMethod(DisplayName = "Whitespace is returned unchanged")]
        public void WhitespaceIsUnchanged()
        {
            Assert.AreEqual("   ", "   ".NormaliseDgtBoardToFen());
        }

        [TestMethod(DisplayName = "A board FEN is returned unchanged")]
        [DataRow(FenBoard.Start)]
        [DataRow(FenBoard.Empty)]
        [DataRow("r3k2r/8/8/8/8/8/8/R3K2R")]
        public void BoardFenUnchanged(string fen)
        {
            Assert.AreEqual(fen, fen.NormaliseDgtBoardToFen());
        }

        [TestMethod(DisplayName = "Only the board field of a full FEN is kept")]
        public void FullFenKeepsBoardField()
        {
            Assert.AreEqual(
                FenBoard.Start,
                $"{FenBoard.Start} w KQkq - 0 1".NormaliseDgtBoardToFen());
        }

        [TestMethod(DisplayName = "The rank-aligned driver form decodes")]
        public void RankAlignedFormDecodes()
        {
            Assert.AreEqual(FenBoard.Start, "rnbqkbnrpppppppp8888PPPPPPPPRNBQKBNR".NormaliseDgtBoardToFen());
        }

        /// <summary>
        /// Captured from a live board on 2026-11-09 - these are the exact strings the
        /// driver emits, with a single run spanning four ranks.
        /// </summary>
        [TestMethod(DisplayName = "The cross-rank driver form decodes")]
        [DataRow("rnbqkbnrpppppppp32PPPPPPPPRNBQKBNR", "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR")]
        [DataRow("rnbqkbnrpppppppp32PPPPPPPP1NBQKBNR", "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/1NBQKBNR")]
        public void CrossRankFormDecodes(string driverString, string expectedFen)
        {
            Assert.AreEqual(expectedFen, driverString.NormaliseDgtBoardToFen());
        }

        [TestMethod(DisplayName = "A board mid-scan is returned unchanged, not as empty squares")]
        [DataRow("rnbq")]
        [DataRow("rnbqkbnrpppppppp")]
        [DataRow("thisfenisjunk!!")]
        [DataRow("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP")]
        public void IncompleteOrJunkIsUnchanged(string input)
        {
            Assert.AreEqual(input, input.NormaliseDgtBoardToFen());
        }

        [TestMethod(DisplayName = "Synthetic driver strings decode back to their board FEN")]
        public void SyntheticDriverStringsRoundTrip()
        {
            const int boards = 10000;

            for (int seed = 1; seed <= boards; seed++)
            {
                char[] flat = SyntheticBoards.RandomBoard(seed, seed % 30);
                string expected = FenBoard.Format(flat);
                string driverString = SyntheticBoards.ToDriverString(flat);

                Assert.AreEqual(expected, driverString.NormaliseDgtBoardToFen(), $"seed {seed}: driver string '{driverString}'");
            }
        }
    }

    [TestClass]
    public class GenerateAlwaysValidMovesFenTests
    {
        [TestMethod(DisplayName = "Turns a full position into a legal position for the side to move")]
        public void StartPositionBecomesPlayable()
        {
            string result = FenBoard.Start.GenrateAlwaysValidMovesFEN(isPlayingWhite: true);

            char[] flat = FenBoard.Parse(result);

            Assert.AreEqual(1, flat.Count(c => c == 'k'), "exactly one black king");
            Assert.AreEqual(0, flat.Count(c => c is 'q' or 'r' or 'b' or 'n' or 'p'), "no other black pieces may survive");
            Assert.AreEqual(
                FenBoard.Parse(FenBoard.Start).Count(c => char.IsUpper(c)),
                flat.Count(c => char.IsUpper(c)),
                "the player's own pieces are preserved");
        }

        [TestMethod(DisplayName = "Works for black as the side to move")]
        public void StartPositionBecomesPlayableForBlack()
        {
            string result = FenBoard.Start.GenrateAlwaysValidMovesFEN(isPlayingWhite: false);

            char[] flat = FenBoard.Parse(result);

            Assert.AreEqual(1, flat.Count(c => c == 'K'), "exactly one white king");
            Assert.AreEqual(0, flat.Count(c => c is 'Q' or 'R' or 'B' or 'N' or 'P'), "no other white pieces may survive");
            Assert.AreEqual(
                FenBoard.Parse(FenBoard.Start).Count(c => char.IsLower(c)),
                flat.Count(c => char.IsLower(c)),
                "the player's own pieces are preserved");
        }

        [TestMethod(DisplayName = "The result is always a well-formed eight-rank board")]
        public void ResultIsWellFormed()
        {
            for (int seed = 1; seed <= 100; seed++)
            {
                string boardFen = FenBoard.Format(SyntheticBoards.RandomBoard(seed, 6));

                string result = boardFen.GenrateAlwaysValidMovesFEN(isPlayingWhite: seed % 2 == 0);

                Assert.AreEqual(64, FenBoard.Parse(result).Length, $"seed {seed}: '{result}'");
            }
        }
    }
}
