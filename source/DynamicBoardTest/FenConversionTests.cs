using DynamicBoard.Helpers;
using DynamicBoardTest.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;

namespace DynamicBoardTest
{
    [TestClass]
    public class FenConversionTests
    {
        [TestMethod(DisplayName = "Board-only FEN matches the independent parser")]
        [DataRow(FenBoard.Start)]
        [DataRow(FenBoard.Empty)]
        [DataRow("r3k2r/8/8/8/8/8/8/R3K2R")]
        [DataRow("8/8/8/8/8/8/8/K6k")]
        public void BoardOnlyFenMatchesIndependentParser(string fen)
        {
            CollectionAssert.AreEqual(FenBoard.Parse(fen), FenConversion.FenToCharArray(fen));
        }

        [TestMethod(DisplayName = "Board-only FENs from synthetic boards match the independent parser")]
        public void SyntheticBoardsMatchIndependentParser()
        {
            for (int seed = 1; seed <= 3000; seed++)
            {
                string boardFen = FenBoard.Format(FenBoard.RandomBoard(seed, seed % 28));

                CollectionAssert.AreEqual(
                    FenBoard.Parse(boardFen),
                    FenConversion.FenToCharArray(boardFen),
                    $"seed {seed}: '{boardFen}'");
            }
        }

        /// <summary>
        /// The two FEN implementations in this solution must agree. ChessHelpers'
        /// FenToCharArray does not isolate the board field; this one does. A test in each
        /// project pins its own implementation's answer so the divergence is visible.
        /// </summary>
        [TestMethod(DisplayName = "A full FEN is read as its board field")]
        [DataRow("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")]
        [DataRow("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1")]
        [DataRow("8/8/8/8/8/8/8/8 w - - 0 1")]
        public void FullFenUsesBoardFieldOnly(string fullFen)
        {
            CollectionAssert.AreEqual(
                FenBoard.Parse(fullFen.Split(' ')[0]),
                FenConversion.FenToCharArray(fullFen));
        }

        [TestMethod(DisplayName = "Surrounding whitespace is ignored")]
        public void SurroundingWhitespaceIsIgnored()
        {
            CollectionAssert.AreEqual(
                FenBoard.Parse(FenBoard.Start),
                FenConversion.FenToCharArray($"  {FenBoard.Start}  "));
        }

        [TestMethod(DisplayName = "Flipping rotates the board 180 degrees")]
        [DataRow("8/8/8/8/8/8/8/K6k")]
        [DataRow("r3k2r/8/8/8/8/8/8/R3K2R")]
        public void FlipRotatesBoard(string fen)
        {
            char[] plain = FenConversion.FenToCharArray(fen);
            char[] flipped = FenConversion.FenToCharArray(fen, isFlipRequired: true);

            char[] expected = new char[64];
            for (int i = 0; i < 64; i++)
            {
                expected[i] = plain[63 - i];
            }

            CollectionAssert.AreEqual(expected, flipped, $"'{fen}'");
        }

        // ------------------------------------------------------ SquareDiffCount

        [TestMethod(DisplayName = "The same position has no difference")]
        public void IdenticalPositionsHaveNoDifference()
        {
            Assert.AreEqual(0, FenConversion.SquareDiffCount(FenBoard.Start, FenBoard.Start));
        }

        [TestMethod(DisplayName = "A quiet move differs on two squares")]
        public void QuietMoveDiffersOnTwoSquares()
        {
            Assert.AreEqual(2, FenConversion.SquareDiffCount(FenBoard.Start, FenBoard.Move(FenBoard.Start, "e2", "e4")));
        }

        [TestMethod(DisplayName = "A capture differs on two squares")]
        public void CaptureDiffersOnTwoSquares()
        {
            const string before = "rnbqkbnr/ppp1pppp/8/3p4/4P3/8/PPPP1PPP/RNBQKBNR";

            Assert.AreEqual(2, FenConversion.SquareDiffCount(before, FenBoard.Move(before, "e4", "d5")));
        }

        [TestMethod(DisplayName = "Castling differs on four squares")]
        public void CastlingDiffersOnFourSquares()
        {
            const string before = "r3k2r/8/8/8/8/8/8/R3K2R";

            char[] flat = FenBoard.Parse(before);
            flat[FenBoard.Index("e1")] = '-';
            flat[FenBoard.Index("g1")] = 'K';
            flat[FenBoard.Index("h1")] = '-';
            flat[FenBoard.Index("f1")] = 'R';

            Assert.AreEqual(4, FenConversion.SquareDiffCount(before, FenBoard.Format(flat)));
        }

        [TestMethod(DisplayName = "The count does not depend on argument order")]
        public void CountIsSymmetric()
        {
            string after = FenBoard.Move(FenBoard.Start, "e2", "e4");

            Assert.AreEqual(
                FenConversion.SquareDiffCount(FenBoard.Start, after),
                FenConversion.SquareDiffCount(after, FenBoard.Start));
        }

        [TestMethod(DisplayName = "The count matches an independent comparison across synthetic pairs")]
        public void CountMatchesIndependentComparison()
        {
            for (int seed = 1; seed <= 2000; seed++)
            {
                string a = FenBoard.Format(FenBoard.RandomBoard(seed, 12));
                string b = FenBoard.Format(FenBoard.RandomBoard(seed + 5000, 12));

                char[] left = FenBoard.Parse(a);
                char[] right = FenBoard.Parse(b);

                int expected = 0;
                for (int i = 0; i < 64; i++)
                {
                    if (left[i] != right[i])
                    {
                        expected++;
                    }
                }

                Assert.AreEqual(expected, FenConversion.SquareDiffCount(a, b), $"seed {seed}");
            }
        }
    }

    [TestClass]
    public class ImageExtensionsTests
    {
        [TestMethod(DisplayName = "A bitmap survives a PNG round-trip")]
        public void BitmapRoundTripsThroughPng()
        {
            using Bitmap original = new(37, 19);
            using (Graphics graphics = Graphics.FromImage(original))
            {
                graphics.Clear(Color.CornflowerBlue);
                graphics.FillRectangle(Brushes.OrangeRed, 3, 4, 11, 9);
            }

            byte[] png = original.ConvertToPngByteArray();

            using Bitmap restored = png.ConvertPngByteArrayToBitmap();

            Assert.AreEqual(37, restored.Width);
            Assert.AreEqual(19, restored.Height);
            Assert.AreEqual(Color.CornflowerBlue.ToArgb(), restored.GetPixel(0, 0).ToArgb());
            Assert.AreEqual(Color.OrangeRed.ToArgb(), restored.GetPixel(5, 6).ToArgb());
        }

        [TestMethod(DisplayName = "The output is a PNG")]
        public void OutputIsPng()
        {
            using Bitmap bitmap = new(4, 4);

            byte[] png = bitmap.ConvertToPngByteArray();

            CollectionAssert.AreEqual(
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },
                png[..8]);
        }
    }
}
