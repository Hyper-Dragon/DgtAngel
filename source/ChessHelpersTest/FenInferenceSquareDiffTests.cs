using ChessHelpers;
using ChessHelpersTest.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace ChessHelpersTest
{
    [TestClass]
    public class FenInferenceSquareDiffTests
    {
        // ---------------------------------------------------------------- quiet moves

        [TestMethod(DisplayName = "A quiet move is identified as a two-square diff")]
        [DataRow("e2", "e4")]
        [DataRow("g1", "f3")]
        [DataRow("b1", "c3")]
        [DataRow("a1", "a3")]
        [DataRow("h7", "h5")]
        [DataRow("e7", "e5")]
        public void QuietMoveIsIdentified(string from, string to)
        {
            string before = FenBoard.Start;
            string after = FenBoard.Move(before, from, to);

            var result = FenInference.SquareDiff(before, after);

            Assert.AreEqual(2, result.diffCount, "a simple move touches exactly two squares");
            Assert.AreEqual(from, result.squareFrom);
            Assert.AreEqual(to, result.squareTo);
            Assert.AreEqual("", result.promotesTo);
        }

        [TestMethod(DisplayName = "A capture is identified as a two-square diff")]
        public void CaptureIsIdentified()
        {
            // White queen takes the black pawn on d5.
            const string before = "rnbqkbnr/ppp1pppp/8/3p4/8/8/PPPPPPPP/RNBQKBNR";
            string after = FenBoard.Move(before, "d1", "d5");

            var result = FenInference.SquareDiff(before, after);

            Assert.AreEqual(2, result.diffCount);
            Assert.AreEqual("d1", result.squareFrom);
            Assert.AreEqual("d5", result.squareTo);
        }

        [TestMethod(DisplayName = "A capture on the last rank is not reported as a promotion")]
        public void BackRankCaptureIsNotPromotion()
        {
            // Rook takes a rook on a8 - two-square diff on the far rank, but not a pawn.
            const string before = "r6R/8/8/8/8/8/8/8";
            string after = FenBoard.Move(before, "h8", "a8");

            var result = FenInference.SquareDiff(before, after);

            Assert.AreEqual(2, result.diffCount);
            Assert.AreEqual("h8", result.squareFrom);
            Assert.AreEqual("a8", result.squareTo);
            Assert.AreEqual("", result.promotesTo, "only pawns promote");
        }

        // ------------------------------------------------------------- en passant

        public static IEnumerable<object[]> EnPassantCases
        {
            get
            {
                foreach (bool white in new[] { true, false })
                {
                    for (int file = 0; file < 8; file++)
                    {
                        string side = white ? "white" : "black";
                        string fileChar = ((char)('a' + file)).ToString();
                        var caseData = SyntheticBoards.EnPassant(white, file);
                        yield return new object[] { white, file, $"{side} en passant onto {fileChar}", caseData.before, caseData.after, caseData.from, caseData.to };
                    }
                }
            }
        }

        [TestMethod(DisplayName = "En passant is identified on every file")]
        [DynamicData(nameof(EnPassantCases))]
        public void EnPassantIsIdentified(bool whiteCaptures, int destinationFile, string label, string before, string after, string from, string to)
        {
            _ = whiteCaptures;
            _ = destinationFile;

            var result = FenInference.SquareDiff(before, after);

            Assert.AreEqual(3, result.diffCount, $"{label}: origin, destination and the captured pawn all change");
            Assert.AreEqual(from, result.squareFrom, $"{label}: origin square");
            Assert.AreEqual(to, result.squareTo, $"{label}: destination square");
            Assert.AreEqual(to, result.fullFromFen.Split(' ')[3], $"{label}: the en-passant target square must be the destination");
        }

        // --------------------------------------------------------------- castling

        [TestMethod(DisplayName = "Castling is identified from a four-square diff")]
        [DataRow("r3k2r/8/8/8/8/8/8/R3K2R", "e1", "g1", "e1", "g1", true)]
        [DataRow("r3k2r/8/8/8/8/8/8/R3K2R", "e1", "c1", "e1", "c1", false)]
        [DataRow("r3k2r/8/8/8/8/8/8/R3K2R", "e8", "g8", "e8", "g8", true)]
        [DataRow("r3k2r/8/8/8/8/8/8/R3K2R", "e8", "c8", "e8", "c8", false)]
        public void CastlingIsIdentified(string board, string kingFrom, string kingTo, string expectedFrom, string expectedTo, bool kingside)
        {
            char[] flat = FenBoard.Parse(board);
            int fromIdx = FenBoard.Index(kingFrom);

            char king = flat[fromIdx];
            bool isWhite = char.IsUpper(king);
            char rook = isWhite ? 'R' : 'r';

            int rankOffset = isWhite ? 0 : 7;
            int rookFromFile = kingside ? 7 : 0;
            int rookToFile = kingside ? 5 : 3;

            char[] moved = (char[])flat.Clone();
            moved[fromIdx] = '-';
            moved[FenBoard.Index(kingTo)] = king;

            int rookFromIdx = (rankOffset * 8) + rookFromFile;
            moved[rookFromIdx] = '-';
            moved[(rankOffset * 8) + rookToFile] = rook;

            string after = FenBoard.Format(moved);

            var result = FenInference.SquareDiff(board, after);

            Assert.AreEqual(4, result.diffCount, "castling changes king and rook squares only");
            Assert.AreEqual(expectedFrom, result.squareFrom);
            Assert.AreEqual(expectedTo, result.squareTo);
        }

        // ------------------------------------------------------------- promotion

        [TestMethod(DisplayName = "A white quiet promotion reports the promotion piece")]
        [DataRow("Q")]
        [DataRow("R")]
        [DataRow("B")]
        [DataRow("N")]
        public void WhiteQuietPromotionReportsPiece(string piece)
        {
            const string before = "8/4P3/8/8/8/8/8/8";
            char[] flat = FenBoard.Parse(before);
            char[] after = (char[])flat.Clone();
            after[FenBoard.Index("e7")] = '-';
            after[FenBoard.Index("e8")] = char.Parse(piece);

            var result = FenInference.SquareDiff(before, FenBoard.Format(after));

            Assert.AreEqual("e7", result.squareFrom);
            Assert.AreEqual("e8", result.squareTo);
            Assert.AreEqual(piece, result.promotesTo, "the destination piece is the promotion piece");
        }

        [TestMethod(DisplayName = "A black quiet promotion reports the promotion piece")]
        [DataRow("q")]
        [DataRow("r")]
        [DataRow("b")]
        [DataRow("n")]
        public void BlackQuietPromotionReportsPiece(string piece)
        {
            const string before = "8/8/8/8/8/8/4p3/8";
            char[] flat = FenBoard.Parse(before);
            char[] after = (char[])flat.Clone();
            after[FenBoard.Index("e2")] = '-';
            after[FenBoard.Index("e1")] = char.Parse(piece);

            var result = FenInference.SquareDiff(before, FenBoard.Format(after));

            Assert.AreEqual("e2", result.squareFrom);
            Assert.AreEqual("e1", result.squareTo);
            Assert.AreEqual(piece.ToUpperInvariant(), result.promotesTo, "the destination piece is the promotion piece");
        }

        [TestMethod(DisplayName = "A white capture promotion reports the promotion piece")]
        public void WhiteCapturePromotionReportsPiece()
        {
            const string before = "7r/4P3/8/8/8/8/8/8";
            char[] flat = FenBoard.Parse(before);
            char[] after = (char[])flat.Clone();
            after[FenBoard.Index("e7")] = '-';
            after[FenBoard.Index("h8")] = 'N';

            var result = FenInference.SquareDiff(before, FenBoard.Format(after));

            Assert.AreEqual("e7", result.squareFrom);
            Assert.AreEqual("h8", result.squareTo);
            Assert.AreEqual("N", result.promotesTo);
        }

        [TestMethod(DisplayName = "A black capture promotion reports the promotion piece")]
        public void BlackCapturePromotionReportsPiece()
        {
            const string before = "8/8/8/8/8/8/4p3/7R";
            char[] flat = FenBoard.Parse(before);
            char[] after = (char[])flat.Clone();
            after[FenBoard.Index("e2")] = '-';
            after[FenBoard.Index("h1")] = 'n';

            var result = FenInference.SquareDiff(before, FenBoard.Format(after));

            Assert.AreEqual("e2", result.squareFrom);
            Assert.AreEqual("h1", result.squareTo);
            Assert.AreEqual("N", result.promotesTo);
        }

        // -------------------------------------------------- inferred FEN metadata

        [TestMethod(DisplayName = "The inferred FEN names the mover as side to move")]
        public void InferredFenNamesMover()
        {
            string after = FenBoard.Move(FenBoard.Start, "e2", "e4");

            var result = FenInference.SquareDiff(FenBoard.Start, after);

            Assert.AreEqual("w", result.fullFromFen.Split(' ')[1], "a white move means the position before it had white to move");
        }

        [TestMethod(DisplayName = "The inferred FEN keeps the mover's castling rights")]
        public void InferredFenKeepsCastlingRights()
        {
            var result = FenInference.SquareDiff(FenBoard.Start, FenBoard.Move(FenBoard.Start, "e2", "e4"));

            Assert.AreEqual("KQkq", result.fullFromFen.Split(' ')[2]);
        }

        [TestMethod(DisplayName = "The inferred FEN uses '-' when there are no castling rights")]
        public void InferredFenHasNoCastlingRights()
        {
            const string before = "8/8/8/8/8/8/4P3/8";
            string after = FenBoard.Move(before, "e2", "e4");

            var result = FenInference.SquareDiff(before, after);

            Assert.AreEqual("-", result.fullFromFen.Split(' ')[2]);
        }

        // ---------------------------------------------------------------- bulk

        [TestMethod(DisplayName = "Quiet moves from synthetic boards are identified exactly")]
        public void QuietMovesFromSyntheticBoards()
        {
            const int boards = 100;
            const int movesPerBoard = 400;

            int checkedMoves = 0;

            for (int seed = 1; seed <= boards; seed++)
            {
                char[] board = SyntheticBoards.RandomBoard(seed, 8);
                string boardFen = FenBoard.Format(board);

                int taken = 0;
                foreach (var move in SyntheticBoards.QuietMoves(boardFen))
                {
                    if (taken++ >= movesPerBoard)
                    {
                        break;
                    }

                    var result = FenInference.SquareDiff(boardFen, move.resultFen);

                    Assert.AreEqual(2, result.diffCount, $"seed {seed}: {move.from}->{move.to} should be a two-square diff");
                    Assert.AreEqual(move.from, result.squareFrom, $"seed {seed}: origin of {move.from}->{move.to}");
                    Assert.AreEqual(move.to, result.squareTo, $"seed {seed}: destination of {move.from}->{move.to}");

                    checkedMoves++;
                }
            }

            Assert.IsTrue(checkedMoves >= boards * movesPerBoard, $"expected at least {boards * movesPerBoard} moves, checked {checkedMoves}");
        }

        [TestMethod(DisplayName = "Captures from synthetic boards are identified exactly")]
        public void CapturesFromSyntheticBoards()
        {
            const int boards = 100;

            int checkedMoves = 0;

            for (int seed = 1; seed <= boards; seed++)
            {
                string boardFen = FenBoard.Format(SyntheticBoards.RandomBoard(seed, 12));

                foreach (var move in SyntheticBoards.Captures(boardFen))
                {
                    var result = FenInference.SquareDiff(boardFen, move.resultFen);

                    Assert.AreEqual(2, result.diffCount, $"seed {seed}: capture {move.from}x{move.to}");
                    Assert.AreEqual(move.from, result.squareFrom, $"seed {seed}: origin of {move.from}x{move.to}");
                    Assert.AreEqual(move.to, result.squareTo, $"seed {seed}: destination of {move.from}x{move.to}");

                    checkedMoves++;
                }
            }

            Assert.IsTrue(checkedMoves > 0, "the generator produced no captures to check");
        }

        // -------------------------------------------------------------- edge cases

        [TestMethod(DisplayName = "A full FEN is accepted - only the board field is used")]
        public void FullFenIsAccepted()
        {
            var result = FenInference.SquareDiff(
                "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
                "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1");

            Assert.AreEqual("e2", result.squareFrom);
            Assert.AreEqual("e4", result.squareTo);
        }
    }
}
