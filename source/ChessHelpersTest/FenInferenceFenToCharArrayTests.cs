using ChessHelpers;
using ChessHelpersTest.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace ChessHelpersTest
{
    [TestClass]
    public class FenInferenceFenToCharArrayTests
    {
        [TestMethod(DisplayName = "Board-only FEN matches the independent parser")]
        [DataRow(FenBoard.Start)]
        [DataRow(FenBoard.Empty)]
        [DataRow("8/8/8/8/8/8/8/K6k")]
        [DataRow("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR")]
        [DataRow("r3k2r/8/8/8/8/8/8/R3K2R")]
        public void BoardOnlyFenMatchesIndependentParser(string fen)
        {
            CollectionAssert.AreEqual(FenBoard.Parse(fen), FenInference.FenToCharArray(fen));
        }

        [TestMethod(DisplayName = "Board-only FENs from synthetic boards match the independent parser")]
        public void SyntheticBoardsMatchIndependentParser()
        {
            const int boards = 5000;

            for (int seed = 1; seed <= boards; seed++)
            {
                string boardFen = FenBoard.Format(SyntheticBoards.RandomBoard(seed, seed % 30));

                CollectionAssert.AreEqual(
                    FenBoard.Parse(boardFen),
                    FenInference.FenToCharArray(boardFen),
                    $"seed {seed}: '{boardFen}'");
            }
        }

        [TestMethod(DisplayName = "An empty string yields an empty board")]
        public void EmptyStringYieldsEmptyBoard()
        {
            char[] flat = FenInference.FenToCharArray("");

            Assert.AreEqual(64, flat.Length);
            CollectionAssert.AreEqual(FenBoard.Parse(FenBoard.Empty), flat);
        }

        [TestMethod(DisplayName = "Flipping rotates the board 180 degrees")]
        [DataRow("8/8/8/8/8/8/8/K6k")]
        [DataRow("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR")]
        [DataRow("r3k2r/8/8/8/8/8/8/R3K2R")]
        public void FlipRotatesBoard180Degrees(string fen)
        {
            char[] plain = FenInference.FenToCharArray(fen);
            char[] flipped = FenInference.FenToCharArray(fen, isFlipRequired: true);

            // An independent oracle: a 180-degree rotation maps index i to 63 - i.
            char[] expected = new char[64];
            for (int i = 0; i < 64; i++)
            {
                expected[i] = plain[63 - i];
            }

            CollectionAssert.AreEqual(expected, flipped, $"'{fen}'");
        }

        [TestMethod(DisplayName = "A blank-only board flips to itself")]
        public void BlankBoardFlipIsIdentity()
        {
            CollectionAssert.AreEqual(
                FenInference.FenToCharArray(FenBoard.Empty),
                FenInference.FenToCharArray(FenBoard.Empty, isFlipRequired: true));
        }

        /// <summary>
        /// Callers in this solution always pass a board-only FEN, and SquareDiff/Normalise
        /// both strip the tail first. A full FEN reaching this method unguarded must still
        /// be interpreted as a board - DynamicBoard.Helpers.FenConversion does this and the
        /// two implementations must not diverge.
        /// </summary>
        [TestMethod(DisplayName = "A full FEN is interpreted as its board field")]
        [DataRow("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")]
        [DataRow("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1")]
        [DataRow("8/8/8/8/8/8/8/8 w - - 0 1")]
        public void FullFenUsesBoardFieldOnly(string fullFen)
        {
            string boardOnly = fullFen.Split(' ')[0];

            CollectionAssert.AreEqual(
                FenBoard.Parse(boardOnly),
                FenInference.FenToCharArray(fullFen),
                $"'{fullFen}' should parse as '{boardOnly}'");
        }

        [TestMethod(DisplayName = "Null input is rejected as a bad argument")]
        public void NullThrows()
        {
            _ = Assert.ThrowsExactly<ArgumentNullException>(() => FenInference.FenToCharArray(null!));
        }

        public static IEnumerable<object[]> OverflowCases
        {
            get
            {
                yield return new object[] { "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR/rnbqkbnr" };
                yield return new object[] { "ppppppppppppppppppppppppppppppppppppppppppppppppppppppppppppppppp" };
            }
        }

        [TestMethod(DisplayName = "A board describing more than 64 squares throws")]
        [DynamicData(nameof(OverflowCases))]
        public void OversizedBoardThrows(string fen)
        {
            _ = Assert.ThrowsExactly<IndexOutOfRangeException>(() => FenInference.FenToCharArray(fen));
        }
    }
}
