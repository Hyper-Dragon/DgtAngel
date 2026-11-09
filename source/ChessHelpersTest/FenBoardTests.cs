using ChessHelpersTest.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChessHelpersTest
{
    /// <summary>
    /// Tests for the test-side FEN helper itself. An unverified oracle is worse than no
    /// oracle, so the helper earns its own coverage before anything relies on it.
    /// </summary>
    [TestClass]
    public class FenBoardTests
    {
        [TestMethod(DisplayName = "Parse/Format round-trips the start position")]
        public void ParseFormatRoundTripsStartPosition()
        {
            char[] flat = FenBoard.Parse(FenBoard.Start);

            Assert.AreEqual(64, flat.Length);
            Assert.AreEqual(FenBoard.Start, FenBoard.Format(flat));
        }

        [TestMethod(DisplayName = "Parse/Format round-trips the empty board")]
        public void ParseFormatRoundTripsEmptyBoard()
        {
            char[] flat = FenBoard.Parse(FenBoard.Empty);

            foreach (char square in flat)
            {
                Assert.AreEqual('-', square);
            }

            Assert.AreEqual(FenBoard.Empty, FenBoard.Format(flat));
        }

        [TestMethod(DisplayName = "Index maps algebraic squares to the board-array convention")]
        [DataRow("a8", 0)]
        [DataRow("h8", 7)]
        [DataRow("e8", 4)]
        [DataRow("a1", 56)]
        [DataRow("e1", 60)]
        [DataRow("h1", 63)]
        public void IndexMapsAlgebraicSquares(string square, int expected)
        {
            Assert.AreEqual(expected, FenBoard.Index(square));
            Assert.AreEqual(square, FenBoard.Square(expected));
        }

        [TestMethod(DisplayName = "Start position pieces land on the documented squares")]
        public void StartPositionPiecePlacement()
        {
            char[] flat = FenBoard.Parse(FenBoard.Start);

            Assert.AreEqual('r', flat[FenBoard.Index("a8")]);
            Assert.AreEqual('k', flat[FenBoard.Index("e8")]);
            Assert.AreEqual('p', flat[FenBoard.Index("e7")]);
            Assert.AreEqual('-', flat[FenBoard.Index("e4")]);
            Assert.AreEqual('P', flat[FenBoard.Index("e2")]);
            Assert.AreEqual('K', flat[FenBoard.Index("e1")]);
            Assert.AreEqual('R', flat[FenBoard.Index("h1")]);
            Assert.AreEqual('-', flat[FenBoard.Index("e5")]);
        }

        [TestMethod(DisplayName = "Move relocates a piece and empties the origin")]
        public void MoveRelocatesPiece()
        {
            string after = FenBoard.Move(FenBoard.Start, "e2", "e4");

            Assert.AreEqual("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR", after);
        }

        [TestMethod(DisplayName = "Occupied lists every piece in board order")]
        public void OccupiedListsPieces()
        {
            var pieces = FenBoard.Occupied(FenBoard.Start);

            Assert.AreEqual(32, pieces.Count);
            Assert.AreEqual("a8", pieces[0].square);
            Assert.AreEqual('r', pieces[0].piece);
        }

        [TestMethod(DisplayName = "Parse rejects a rank that does not describe eight squares")]
        public void ParseRejectsBadRank()
        {
            Assert.ThrowsExactly<System.ArgumentException>(() => FenBoard.Parse("rnbqkbn/8/8/8/8/8/8/8"));
        }

        [TestMethod(DisplayName = "Parse rejects the wrong number of ranks")]
        public void ParseRejectsBadRankCount()
        {
            Assert.ThrowsExactly<System.ArgumentException>(() => FenBoard.Parse("8/8/8/8/8/8/8"));
        }
    }
}
