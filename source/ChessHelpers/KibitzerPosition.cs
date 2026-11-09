using System.Globalization;

namespace ChessHelpers
{
    /// <summary>
    /// What an engine, or a kibitzer, needs to know about the position on the board: whose turn
    /// it is, what castling is still possible, and whether en passant is on.
    /// </summary>
    /// <remarks>
    /// A DGT board reports only where the pieces are. Everything else has to be inferred, and the
    /// two sources are different:
    ///
    ///   - Castling rights come from the current position alone. If the king or the rook has left
    ///     its starting square the right is gone, and on a physical board that can never be
    ///     undone, so looking at the squares is as good as keeping the history.
    ///
    ///   - En passant needs the previous position, because the target square exists only for the
    ///     move immediately after a double pawn push.
    ///
    ///   - Whose turn it is also needs the previous position: the side to play is the opposite of
    ///     the side whose piece last appeared. With nothing yet moved, white is to play.
    /// </remarks>
    public sealed class KibitzerPosition
    {
        private const int SquaresOnBoard = 8;
        private const char EmptySquare = '-';

        private static readonly char[] FileNames = "abcdefgh".ToCharArray();
        private static readonly char[] RankNames = "87654321".ToCharArray();

        private const int A8 = 0;
        private const int E8 = 4;
        private const int H8 = 7;
        private const int A1 = 56;
        private const int E1 = 60;
        private const int H1 = 63;

        public string Board { get; }
        public bool IsWhiteToPlay { get; }
        public bool WhiteCanCastleShort { get; }
        public bool WhiteCanCastleLong { get; }
        public bool BlackCanCastleShort { get; }
        public bool BlackCanCastleLong { get; }

        /// <summary>The square a pawn would be captured on, or "-" when en passant is not on.</summary>
        public string EnPassantSquare { get; }

        private KibitzerPosition(string board, bool isWhiteToPlay, string castle, string enPassantSquare)
        {
            Board = board;
            IsWhiteToPlay = isWhiteToPlay;
            EnPassantSquare = enPassantSquare;

            WhiteCanCastleShort = castle.Contains('K', StringComparison.Ordinal);
            WhiteCanCastleLong = castle.Contains('Q', StringComparison.Ordinal);
            BlackCanCastleShort = castle.Contains('k', StringComparison.Ordinal);
            BlackCanCastleLong = castle.Contains('q', StringComparison.Ordinal);
        }

        /// <summary>The castling field of the FEN, or "-" when neither side can castle.</summary>
        public string CastleField
        {
            get
            {
                string castle = $"{(WhiteCanCastleShort ? "K" : "")}{(WhiteCanCastleLong ? "Q" : "")}" +
                                $"{(BlackCanCastleShort ? "k" : "")}{(BlackCanCastleLong ? "q" : "")}";

                return string.IsNullOrEmpty(castle) ? "-" : castle;
            }
        }

        /// <summary>A complete FEN for the engine, with the inferred fields filled in.</summary>
        public string Fen => string.Join(' ',
            Board,
            IsWhiteToPlay ? "w" : "b",
            CastleField,
            EnPassantSquare,
            "0",
            "1");

        /// <summary>
        /// Infers the position from the board as it is now and the board as it was before the last
        /// move was made.
        /// </summary>
        /// <param name="currentBoard">The board now.</param>
        /// <param name="previousBoard">
        /// The board before the last move. Empty when there is no history, which is treated as the
        /// initial position: white to play, all castling rights, no en passant.
        /// </param>
        public static KibitzerPosition Infer(string currentBoard, string previousBoard)
        {
            ArgumentNullException.ThrowIfNull(currentBoard);

            char[] current = BoardSquares(currentBoard);
            char[] previous = BoardSquares(previousBoard);

            return new KibitzerPosition(
                BoardField(currentBoard),
                InferWhiteToPlay(current, previous),
                InferCastling(current),
                InferEnPassant(current, previous));
        }

        private static string BoardField(string fen)
        {
            // Callers pass a board, but be forgiving about a full FEN arriving by mistake.
            return fen.Split(' ')[0].Trim();
        }

        private static char[] BoardSquares(string? fen)
        {
            return string.IsNullOrWhiteSpace(fen)
                ? EmptyBoard()
                : FenInference.FenToCharArray(fen);
        }

        private static char[] EmptyBoard()
        {
            char[] squares = new char[SquaresOnBoard * SquaresOnBoard];
            Array.Fill(squares, EmptySquare);

            return squares;
        }

        /// <summary>
        /// The side to play is the opposite of the side whose piece last appeared on the board.
        /// </summary>
        /// <remarks>
        /// That one rule covers ordinary moves, captures, castling (two pieces, one colour),
        /// promotion and en passant captures, because in every case the only pieces that appear
        /// belong to the side that moved. Counting pieces instead - which is what this replaced -
        /// cannot work: a quiet move leaves the counts untouched.
        /// </remarks>
        private static bool InferWhiteToPlay(char[] current, char[] previous)
        {
            bool whiteAppeared = false;
            bool blackAppeared = false;

            for (int square = 0; square < current.Length; square++)
            {
                if (current[square] == previous[square]) { continue; }

                // A square that emptied is where something came from, not what moved. A captured
                // piece vanishes the same way, so neither tells us whose turn it is.
                if (current[square] == EmptySquare) { continue; }

                if (char.IsUpper(current[square])) { whiteAppeared = true; }
                else if (char.IsLower(current[square])) { blackAppeared = true; }
            }

            if (whiteAppeared && !blackAppeared) { return false; }
            if (blackAppeared && !whiteAppeared) { return true; }

            // Nothing moved, or both colours moved, which a single move cannot do.
            return true;
        }

        /// <summary>
        /// Castling rights from where the king and rooks are now.
        /// </summary>
        /// <remarks>
        /// On a DGT board a piece that has left its starting square never returns to it as an
        /// unrevealed discovery, so the squares are the whole story - there is no need to keep a
        /// move history to know a right has been lost.
        /// </remarks>
        private static string InferCastling(char[] current)
        {
            bool whiteKingHome = current[E1] == 'K';
            bool blackKingHome = current[E8] == 'k';

            string castle =
                $"{(whiteKingHome && current[H1] == 'R' ? "K" : "")}" +
                $"{(whiteKingHome && current[A1] == 'R' ? "Q" : "")}" +
                $"{(blackKingHome && current[H8] == 'r' ? "k" : "")}" +
                $"{(blackKingHome && current[A8] == 'r' ? "q" : "")}";

            return string.IsNullOrEmpty(castle) ? "-" : castle;
        }

        /// <summary>
        /// The en passant target, which exists only for the position straight after a double
        /// pawn push: the square the pawn jumped over.
        /// </summary>
        /// <remarks>
        /// This is the FEN convention rather than a statement that a capture is actually
        /// available - an engine is entitled to the square whenever a pawn has just gone two, and
        /// it works out for itself whether anything can take.
        /// </remarks>
        private static string InferEnPassant(char[] current, char[] previous)
        {
            for (int file = 0; file < SquaresOnBoard; file++)
            {
                // White: rank 2 to rank 4, passing rank 3.
                int whiteFrom = (6 * SquaresOnBoard) + file;
                int whiteTo = (4 * SquaresOnBoard) + file;

                if (previous[whiteFrom] == 'P' && current[whiteFrom] != 'P' && current[whiteTo] == 'P')
                {
                    return SquareName((5 * SquaresOnBoard) + file);
                }

                // Black: rank 7 to rank 5, passing rank 6.
                int blackFrom = (1 * SquaresOnBoard) + file;
                int blackTo = (3 * SquaresOnBoard) + file;

                if (previous[blackFrom] == 'p' && current[blackFrom] != 'p' && current[blackTo] == 'p')
                {
                    return SquareName((2 * SquaresOnBoard) + file);
                }
            }

            return "-";
        }

        private static string SquareName(int square)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{FileNames[square % SquaresOnBoard]}{RankNames[square / SquaresOnBoard]}");
        }
    }
}
