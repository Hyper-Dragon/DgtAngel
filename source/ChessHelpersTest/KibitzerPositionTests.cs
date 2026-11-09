using ChessHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChessHelpersTest
{
    /// <summary>
    /// Inference of the position facts a DGT board cannot report: whose turn it is, what castling
    /// is still possible, and whether en passant is on.
    /// </summary>
    /// <remarks>
    /// The board knows only where the pieces are. Everything here is derived, and the two sources
    /// are different: castling comes from the current squares, while the turn and en passant need
    /// the position before the last move.
    /// </remarks>
    [TestClass]
    public class KibitzerPositionTests
    {
        private const string Initial = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";
        private const string AfterE4 = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR";
        private const string AfterE4E5 = "rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR";
        private const string AfterE4E5Nf3 = "rnbqkbnr/pppp1ppp/8/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R";

        [TestMethod(DisplayName = "With nothing moved it is white to play")]
        public void InitialPositionIsWhiteToPlay()
        {
            KibitzerPosition position = KibitzerPosition.Infer(Initial, "");

            Assert.IsTrue(position.IsWhiteToPlay, "the initial position is white to play");
            Assert.AreEqual("KQkq", position.CastleField, "nobody has moved, so all four rights remain");
            Assert.AreEqual("-", position.EnPassantSquare);
        }

        [TestMethod(DisplayName = "After a white move it is black to play")]
        public void WhiteMoveGivesBlackTheTurn()
        {
            KibitzerPosition position = KibitzerPosition.Infer(AfterE4, Initial);

            Assert.IsFalse(position.IsWhiteToPlay, "white moved, so it is black's turn");
        }

        [TestMethod(DisplayName = "After a black move it is white to play")]
        public void BlackMoveGivesWhiteTheTurn()
        {
            KibitzerPosition position = KibitzerPosition.Infer(AfterE4E5, AfterE4);

            Assert.IsTrue(position.IsWhiteToPlay, "black moved, so it is white's turn");
        }

        [TestMethod(DisplayName = "A quiet move is read from the squares, not the piece counts")]
        public void QuietMoveIsDetected()
        {
            // A knight move leaves every piece count untouched, which is why counting cannot work.
            KibitzerPosition position = KibitzerPosition.Infer(AfterE4E5Nf3, AfterE4E5);

            Assert.IsFalse(position.IsWhiteToPlay, "white moved a knight, so it is black's turn");
        }

        [TestMethod(DisplayName = "A white double pawn push opens en passant on the crossed square")]
        public void WhiteDoublePushSetsEnPassant()
        {
            KibitzerPosition position = KibitzerPosition.Infer(AfterE4, Initial);

            Assert.AreEqual("e3", position.EnPassantSquare, "the pawn crossed e3");
        }

        [TestMethod(DisplayName = "A black double pawn push opens en passant on the crossed square")]
        public void BlackDoublePushSetsEnPassant()
        {
            KibitzerPosition position = KibitzerPosition.Infer(AfterE4E5, AfterE4);

            Assert.AreEqual("e6", position.EnPassantSquare, "the pawn crossed e6");
        }

        [TestMethod(DisplayName = "A single pawn push does not open en passant")]
        public void SinglePawnPushDoesNotSetEnPassant()
        {
            const string afterE4E6 = "rnbqkbnr/pppp1ppp/4p3/8/4P3/8/PPPP1PPP/RNBQKBNR";

            KibitzerPosition position = KibitzerPosition.Infer(afterE4E6, AfterE4);

            Assert.AreEqual("-", position.EnPassantSquare, "only a two-square push opens en passant");
        }

        [TestMethod(DisplayName = "En passant lasts only for the move straight after the push")]
        public void EnPassantIsNotCarriedForward()
        {
            KibitzerPosition position = KibitzerPosition.Infer(AfterE4E5Nf3, AfterE4E5);

            Assert.AreEqual("-", position.EnPassantSquare,
                            "the right expired when white played something else");
        }

        [TestMethod(DisplayName = "A capture is still detected as a white move")]
        public void CaptureIsDetected()
        {
            const string afterE4E5D4 = "rnbqkbnr/pppp1ppp/8/4p3/3PP3/8/PPP2PPP/RNBQKBNR";
            const string afterExd4 = "rnbqkbnr/pppp1ppp/8/8/3pP3/8/PPP2PPP/RNBQKBNR";

            KibitzerPosition position = KibitzerPosition.Infer(afterExd4, afterE4E5D4);

            Assert.IsTrue(position.IsWhiteToPlay, "black captured, so it is white's turn");
        }

        [TestMethod(DisplayName = "Castling is detected from the king and rook having moved")]
        public void CastlingMoveIsDetected()
        {
            const string rooksOnly = "r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R";
            const string afterWhiteCastles = "r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R4RK1";

            KibitzerPosition position = KibitzerPosition.Infer(afterWhiteCastles, rooksOnly);

            Assert.IsFalse(position.IsWhiteToPlay, "white castled, so it is black's turn");
            Assert.IsFalse(position.WhiteCanCastleShort, "the king has left e1");
            Assert.IsFalse(position.WhiteCanCastleLong, "the king has left e1");
            Assert.IsTrue(position.BlackCanCastleShort, "black has not moved");
            Assert.IsTrue(position.BlackCanCastleLong, "black has not moved");
        }

        [TestMethod(DisplayName = "Moving a rook loses that side's castling and keeps the other")]
        public void RookMoveLosesOneRight()
        {
            const string afterRookLeaves = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBN1";

            KibitzerPosition position = KibitzerPosition.Infer(afterRookLeaves, Initial);

            Assert.IsTrue(position.WhiteCanCastleLong, "the queen's rook is still on a1");
            Assert.IsFalse(position.WhiteCanCastleShort, "the king's rook has left h1");
        }

        [TestMethod(DisplayName = "A king that has moved loses both rights")]
        public void KingMoveLosesBothRights()
        {
            const string afterKingLeaves = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQ1RKR";

            KibitzerPosition position = KibitzerPosition.Infer(afterKingLeaves, Initial);

            Assert.IsFalse(position.WhiteCanCastleShort, "both rooks are home but the king is not");
            Assert.IsFalse(position.WhiteCanCastleLong, "both rooks are home but the king is not");
            Assert.AreEqual("kq", position.CastleField);
        }

        [TestMethod(DisplayName = "Black castling rights are inferred independently")]
        public void BlackCastlingRightsAreIndependent()
        {
            const string afterRookLeaves = "1nbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";

            KibitzerPosition position = KibitzerPosition.Infer(afterRookLeaves, Initial);

            Assert.IsTrue(position.WhiteCanCastleShort, "white has not moved");
            Assert.IsTrue(position.WhiteCanCastleLong, "white has not moved");
            Assert.IsFalse(position.BlackCanCastleLong, "the queen's rook has left a8");
            Assert.IsTrue(position.BlackCanCastleShort, "the king's rook is still on h8");
        }

        [TestMethod(DisplayName = "The FEN carries the inferred fields in the right order")]
        public void FenCarriesTheInferredFields()
        {
            KibitzerPosition position = KibitzerPosition.Infer(AfterE4E5, AfterE4);

            Assert.AreEqual($"{AfterE4E5} w KQkq e6 0 1", position.Fen);
        }

        [TestMethod(DisplayName = "Castling field reads as a dash when nothing is left")]
        public void NoCastlingReadsAsDash()
        {
            const string noRights = "4k3/8/8/8/8/8/8/4K3";

            KibitzerPosition position = KibitzerPosition.Infer(noRights, "");

            Assert.AreEqual("-", position.CastleField, "neither king is on its starting square");
            Assert.IsFalse(position.WhiteCanCastleShort || position.WhiteCanCastleLong);
            Assert.IsFalse(position.BlackCanCastleShort || position.BlackCanCastleLong);
        }

        [TestMethod(DisplayName = "A full FEN passed by mistake is read as the board only")]
        public void FullFenIsTolerated()
        {
            KibitzerPosition position = KibitzerPosition.Infer($"{AfterE4E5} w KQkq e6 0 1", AfterE4);

            Assert.AreEqual(AfterE4E5, position.Board, "only the board field describes squares");
        }

        [TestMethod(DisplayName = "A scan with a piece lifted is recognised as a piece in hand")]
        public void PieceInHandIsRecognised()
        {
            // The b7 pawn has left the board and nothing has arrived yet.
            Assert.IsTrue(KibitzerPosition.IsPieceInHand(Initial, "rnbqkbnr/p1pppppp/8/8/8/8/PPPPPPPP/RNBQKBNR"),
                          "something left and nothing arrived, so the piece is in the air");
        }

        [TestMethod(DisplayName = "Putting the piece down is not a piece in hand")]
        public void PlacementIsNotPieceInHand()
        {
            const string beforeLift = "rnbqkbnr/p1pppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";
            const string placedOnB5 = "rnbqkbnr/p1pppppp/8/1p6/8/8/PPPPPPPP/RNBQKBNR";

            Assert.IsFalse(KibitzerPosition.IsPieceInHand(beforeLift, placedOnB5),
                           "the pawn arrived, so the move is complete");
        }

        [TestMethod(DisplayName = "A capture is a placement, not a piece in hand")]
        public void CaptureIsNotPieceInHand()
        {
            // Black pawn b5 takes a white pawn on c4: something arrived, something left.
            Assert.IsFalse(KibitzerPosition.IsPieceInHand("8/8/8/1p6/2P5/8/8/8", "8/8/8/8/1p6/8/8/8"));
        }

        [TestMethod(DisplayName = "An unchanged board is not a piece in hand")]
        public void UnchangedBoardIsNotPieceInHand()
        {
            Assert.IsFalse(KibitzerPosition.IsPieceInHand(Initial, Initial));
            Assert.IsFalse(KibitzerPosition.IsPieceInHand("", Initial), "with no history there is nothing to compare");
        }

        [TestMethod(DisplayName = "A double push still opens en passant when a lift was seen in between")]
        public void DoublePushAcrossALiftOpensEnPassant()
        {
            // What a physical board actually reports for b7-b5: the settled position, then the
            // pawn in the air, then the pawn on b5. Measuring from the middle scan loses the move.
            const string beforeMove = Initial;
            const string lifted = "rnbqkbnr/p1pppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";
            const string afterMove = "rnbqkbnr/p1pppppp/8/1p6/8/8/PPPPPPPP/RNBQKBNR";

            Assert.IsTrue(KibitzerPosition.IsPieceInHand(beforeMove, lifted));

            KibitzerPosition position = KibitzerPosition.Infer(afterMove, beforeMove);

            Assert.AreEqual("b6", position.EnPassantSquare, "the pawn crossed b6");
            Assert.IsTrue(position.IsWhiteToPlay, "black has just moved");
        }
    }
}
