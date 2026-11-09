using ChessHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChessHelpersTest
{
    /// <summary>
    /// Engine variations rendered for a human.
    /// </summary>
    /// <remarks>
    /// Engines report a principal variation as square-to-square moves, which is precise and
    /// unreadable. These tests use real variations from Rybka so the formats are the ones that
    /// actually arrive.
    /// </remarks>
    [TestClass]
    public class SanFormatterTests
    {
        private const string InitialFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        private static string[] Moves(string line) => line.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

        [TestMethod(DisplayName = "A knight opening reads as piece symbols rather than coordinates")]
        public void KnightOpeningIsTranslated()
        {
            string notation = SanFormatter.ToNotation(InitialFen,
                Moves("e2e4 e7e5 b1c3 g8f6 g1f3 b8c6 f1b5 f8c5"), 8);

            Assert.AreEqual("e4 e5 \u2658c3 \u265Ef6 \u2658f3 \u265Ec6 \u2657b5 \u265Dc5", notation,
                "white pieces use white symbols and black pieces black ones");
        }

        [TestMethod(DisplayName = "Pawn moves are bare squares")]
        public void PawnMovesAreBare()
        {
            string notation = SanFormatter.ToNotation(InitialFen,
                Moves("d2d4 g8f6 b1c3 d7d5 g1f3 g7g6 g2g3 b8c6"), 8);

            Assert.AreEqual("d4 \u265Ef6 \u2658c3 d5 \u2658f3 g6 g3 \u265Ec6", notation);
        }

        [TestMethod(DisplayName = "A second variation is translated the same way")]
        public void SecondVariationIsTranslated()
        {
            string notation = SanFormatter.ToNotation(InitialFen,
                Moves("e2e4 e7e5 b1c3 g8f6 g1f3 b8c6 f1c4 f8c5"), 8);

            Assert.AreEqual("e4 e5 \u2658c3 \u265Ef6 \u2658f3 \u265Ec6 \u2657c4 \u265Dc5", notation);
        }

        [TestMethod(DisplayName = "Castling reads as the castling symbol")]
        public void CastlingIsTranslated()
        {
            const string rooksOnly = "r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R w KQkq - 0 1";

            Assert.AreEqual("O-O", SanFormatter.ToNotation(rooksOnly, Moves("e1g1"), 1));
            Assert.AreEqual("O-O-O", SanFormatter.ToNotation(rooksOnly, Moves("e1c1"), 1));
        }

        [TestMethod(DisplayName = "A capture shows the from-file and an x")]
        public void CaptureIsTranslated()
        {
            const string afterE4D5 = "rnbqkbnr/ppp1pppp/8/3p4/4P3/8/PPPP1PPP/RNBQKBNR w KQkq - 0 1";

            Assert.AreEqual("exd5", SanFormatter.ToNotation(afterE4D5, Moves("e4d5"), 1));
        }

        [TestMethod(DisplayName = "A mating move keeps its mate marker")]
        public void MateMarkerIsKept()
        {
            // Fool's mate: 1.f3 e5 2.g4 Qh4#
            const string beforeFoolsMate = "rnbqkbnr/pppp1ppp/8/4p3/6P1/5P2/PPPPP2P/RNBQKBNR b KQkq - 0 1";

            Assert.AreEqual("\u265Bh4#", SanFormatter.ToNotation(beforeFoolsMate, Moves("d8h4"), 1),
                "the black queen uses the black symbol and the mate marker is kept");
        }

        [TestMethod(DisplayName = "A checking move keeps its check marker")]
        public void CheckMarkerIsKept()
        {
            // 1.e4 f5 2.f4 g5 - Qh5 is check, but g6 blocks, so it is not mate.
            const string afterE4F5F4G5 = "rnbqkbnr/pppp3p/8/5pp1/4PP2/8/PPPP2PP/RNBQKBNR w KQkq g6 0 1";

            Assert.AreEqual("\u2655h5+", SanFormatter.ToNotation(afterE4F5F4G5, Moves("d1h5"), 1),
                "the white queen uses the white symbol and the check marker is kept");
        }

        [TestMethod(DisplayName = "Two pieces that can reach the same square are told apart")]
        public void AmbiguousPieceIsDisambiguated()
        {
            // Both knights can reach d2, so the move needs the file to identify it.
            const string twoKnights = "4k3/8/8/8/8/5N2/8/1N2K3 w - - 0 1";

            Assert.AreEqual("\u2658bd2", SanFormatter.ToNotation(twoKnights, Moves("b1d2"), 1));
        }

        [TestMethod(DisplayName = "The line is cut off at the requested number of moves")]
        public void LineIsTruncated()
        {
            string notation = SanFormatter.ToNotation(InitialFen,
                Moves("e2e4 e7e5 b1c3 g8f6 g1f3 b8c6 f1b5 f8c5"), 3);

            Assert.AreEqual("e4 e5 \u2658c3", notation);
        }

        [TestMethod(DisplayName = "A move that will not play stops the line rather than inventing one")]
        public void IllegalMoveStopsTheLine()
        {
            // The rook's own pawn is still on a2, so a1a3 cannot be played.
            string notation = SanFormatter.ToNotation(InitialFen, Moves("e2e4 e7e5 a1a3"), 8);

            Assert.AreEqual("e4 e5", notation, "the illegal move must not be rendered");
        }

        [TestMethod(DisplayName = "Garbage in yields nothing out")]
        public void GarbageYieldsNothing()
        {
            Assert.AreEqual("", SanFormatter.ToNotation(InitialFen, Moves("zzzz"), 8));
            Assert.AreEqual("", SanFormatter.ToNotation("not a fen at all", Moves("e2e4"), 8));
            Assert.AreEqual("", SanFormatter.ToNotation(InitialFen, Moves(""), 8));
        }
    }
}
