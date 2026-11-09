using ChessHelpers;
using ChessHelpersTest.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChessHelpersTest
{
    /// <summary>
    /// CalculateSanFromFen turns two consecutive positions into the move between them.
    /// Expectations here come from the rules of algebraic notation, not from the
    /// implementation - a test that only asserts what the code already does would pass
    /// for a wrong move, which is the one thing this feature must never do.
    /// </summary>
    [TestClass]
    public class PositionDiffCalculatorTests
    {
        // ------------------------------------------------------------ basic moves

        [TestMethod(DisplayName = "A pawn push is named by its destination square")]
        public void PawnPush()
        {
            string after = FenBoard.Move(FenBoard.Start, "e2", "e4");

            var (move, ending, turn) = PositionDiffCalculator.CalculateSanFromFen(FenBoard.Start, after);

            Assert.AreEqual("e4", move);
            Assert.AreEqual("", ending, "the game is not over");
            Assert.AreEqual("BLACK", turn, "white has just moved");
        }

        [TestMethod(DisplayName = "A piece move is named with its letter")]
        [DataRow("g1", "f3", "Nf3")]
        [DataRow("b1", "c3", "Nc3")]
        [DataRow("e2", "e3", "e3")]
        [DataRow("a2", "a4", "a4")]
        [DataRow("h2", "h4", "h4")]
        public void PieceMove(string from, string to, string expectedSan)
        {
            string after = FenBoard.Move(FenBoard.Start, from, to);

            var (move, _, _) = PositionDiffCalculator.CalculateSanFromFen(FenBoard.Start, after);

            Assert.AreEqual(expectedSan, move);
        }

        [TestMethod(DisplayName = "A pawn capture uses the origin file")]
        public void PawnCapture()
        {
            const string before = "rnbqkbnr/ppp1pppp/8/3p4/4P3/8/PPPP1PPP/RNBQKBNR";
            string after = FenBoard.Move(before, "e4", "d5");

            var (move, _, _) = PositionDiffCalculator.CalculateSanFromFen(before, after);

            Assert.AreEqual("exd5", move);
        }

        [TestMethod(DisplayName = "A piece capture is marked with x")]
        public void PieceCapture()
        {
            // d-file clear so the queen has a legal path to the pawn on d5.
            const string before = "4k3/8/8/3p4/8/8/8/3QK3";
            string after = FenBoard.Move(before, "d1", "d5");

            var (move, _, _) = PositionDiffCalculator.CalculateSanFromFen(before, after);

            Assert.AreEqual("Qxd5", move);
        }

        // ------------------------------------------------------------- castling

        [TestMethod(DisplayName = "Kingside castling is O-O")]
        public void KingsideCastling()
        {
            const string before = "r3k2r/8/8/8/8/8/8/R3K2R";

            char[] flat = FenBoard.Parse(before);
            char[] moved = (char[])flat.Clone();
            moved[FenBoard.Index("e1")] = '-';
            moved[FenBoard.Index("g1")] = 'K';
            moved[FenBoard.Index("h1")] = '-';
            moved[FenBoard.Index("f1")] = 'R';

            var (move, _, _) = PositionDiffCalculator.CalculateSanFromFen(before, FenBoard.Format(moved));

            Assert.AreEqual("O-O", move);
        }

        [TestMethod(DisplayName = "Queenside castling is O-O-O")]
        public void QueensideCastling()
        {
            const string before = "r3k2r/8/8/8/8/8/8/R3K2R";

            char[] flat = FenBoard.Parse(before);
            char[] moved = (char[])flat.Clone();
            moved[FenBoard.Index("e1")] = '-';
            moved[FenBoard.Index("c1")] = 'K';
            moved[FenBoard.Index("a1")] = '-';
            moved[FenBoard.Index("d1")] = 'R';

            var (move, _, _) = PositionDiffCalculator.CalculateSanFromFen(before, FenBoard.Format(moved));

            Assert.AreEqual("O-O-O", move);
        }

        // ------------------------------------------------------------ promotion

        [TestMethod(DisplayName = "A promotion names the piece promoted to")]
        public void PromotionToQueen()
        {
            // Black king on h5: the new queen on b8 does not check it, so the SAN carries no +.
            const string before = "8/1P6/8/7k/8/8/8/4K3";
            const string after = "1Q6/8/8/7k/8/8/8/4K3";

            var (move, _, _) = PositionDiffCalculator.CalculateSanFromFen(before, after);

            Assert.AreEqual("b8=Q", move);
        }

        [TestMethod(DisplayName = "An underpromotion names the piece promoted to")]
        public void UnderpromotionToKnight()
        {
            const string before = "8/1P6/8/7k/8/8/8/4K3";
            const string after = "1N6/8/8/7k/8/8/8/4K3";

            var (move, _, _) = PositionDiffCalculator.CalculateSanFromFen(before, after);

            Assert.AreEqual("b8=N", move);
        }

        // -------------------------------------------------------------- endings

        [TestMethod(DisplayName = "Checkmate is reported as a result for the winner")]
        public void CheckmateReportsEnding()
        {
            // Fool's mate: 1.f3 e5 2.g4 Qh4#
            const string before = "rnbqkbnr/pppp1ppp/8/4p3/6P1/5P2/PPPPP2P/RNBQKBNR";
            const string after = "rnb1kbnr/pppp1ppp/8/4p3/6Pq/5P2/PPPPP2P/RNBQKBNR";

            var (move, ending, turn) = PositionDiffCalculator.CalculateSanFromFen(before, after);

            Assert.AreEqual("Qh4#", move, "the mating move is checkmate, so it carries #");
            Assert.AreEqual("0-1", ending, "black delivered mate");
            Assert.AreEqual("WHITE", turn);
        }

        [TestMethod(DisplayName = "A normal move reports no ending")]
        public void NormalMoveReportsNoEnding()
        {
            string after = FenBoard.Move(FenBoard.Start, "e2", "e4");

            var (_, ending, _) = PositionDiffCalculator.CalculateSanFromFen(FenBoard.Start, after);

            Assert.AreEqual("", ending);
        }

        // ------------------------------------------------------------ no answer

        [TestMethod(DisplayName = "Two identical positions produce no move")]
        public void IdenticalPositionsProduceNoMove()
        {
            var (move, ending, turn) = PositionDiffCalculator.CalculateSanFromFen(FenBoard.Start, FenBoard.Start);

            Assert.AreEqual("", move);
            Assert.AreEqual("", ending);
            Assert.AreEqual("", turn);
        }

        [TestMethod(DisplayName = "A missing 'from' position produces no move")]
        public void MissingFromPositionProducesNoMove()
        {
            var (move, ending, turn) = PositionDiffCalculator.CalculateSanFromFen("", FenBoard.Start);

            Assert.AreEqual("", move);
            Assert.AreEqual("", ending);
            Assert.AreEqual("", turn);
        }

        [TestMethod(DisplayName = "Two positions several moves apart produce no move")]
        public void TwoMovesApartProducesNoMove()
        {
            // Nothing legitimately moved between these, so there is no single move to name.
            const string before = "8/8/8/8/8/8/8/8";
            const string after = "8/8/8/8/8/8/8/8";

            var (move, _, _) = PositionDiffCalculator.CalculateSanFromFen(before, after);

            Assert.AreEqual("", move);
        }

        // ----------------------------------------------------------- bulk turns

        [TestMethod(DisplayName = "The reported side to move alternates with each ply")]
        public void TurnAlternates()
        {
            string position = FenBoard.Start;
            string expectedTurn = "BLACK";

            string[] moves =
            {
                "e2:e4", "e7:e5", "g1:f3", "b8:c6", "f1:c4", "g8:f6", "e1:g1",
            };

            foreach (string encoded in moves)
            {
                string[] parts = encoded.Split(':');
                string next = FenBoard.Move(position, parts[0], parts[1]);

                var (move, _, turn) = PositionDiffCalculator.CalculateSanFromFen(position, next);

                Assert.AreNotEqual("", move, $"{encoded} should be identifiable");
                Assert.AreEqual(expectedTurn, turn, $"after {encoded}");
                Assert.IsFalse(move.StartsWith('='), $"'{move}' is not valid SAN");

                position = next;
                expectedTurn = expectedTurn == "BLACK" ? "WHITE" : "BLACK";
            }
        }

        [TestMethod(DisplayName = "No move ever comes back with a dangling promotion marker")]
        public void NoDanglingPromotionMarker()
        {
            const string before = "4k3/1P6/8/8/8/8/8/4K3";

            string[] promotions = { "1Q2k3", "1R2k3", "1B2k3", "1N2k3" };

            foreach (string board in promotions)
            {
                var (move, _, _) = PositionDiffCalculator.CalculateSanFromFen(before, board);

                Assert.IsFalse(move.EndsWith('='), $"'{move}' ends with a dangling '=' for board '{board}'");
            }
        }
    }
}
