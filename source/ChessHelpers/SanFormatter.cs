using Chess;

namespace ChessHelpers
{
    /// <summary>
    /// Turns an engine's principal variation into something a player can read.
    /// </summary>
    /// <remarks>
    /// Engines speak UCI: "g1f3" is a square to a square. That is unambiguous and useless on a
    /// broadcast screen, so the move list is replayed on a real board and each move is converted
    /// to standard algebraic notation with the piece shown as its symbol - "♘f3", "e4", "exd5",
    /// "O-O". The board does the work for disambiguation and for check and mate markers, which is
    /// the reason for using a move generator rather than string handling.
    /// </remarks>
    public static class SanFormatter
    {
        private static readonly Dictionary<char, char> WhiteGlyphs = new()
        {
            ['K'] = '\u2654', ['Q'] = '\u2655', ['R'] = '\u2656', ['B'] = '\u2657', ['N'] = '\u2658',
        };

        private static readonly Dictionary<char, char> BlackGlyphs = new()
        {
            ['K'] = '\u265A', ['Q'] = '\u265B', ['R'] = '\u265C', ['B'] = '\u265D', ['N'] = '\u265E',
        };

        /// <summary>
        /// Renders a UCI move list as notation, giving up quietly at the first move that will not
        /// play on the board.
        /// </summary>
        /// <param name="fen">The position the moves were calculated from.</param>
        /// <param name="uciMoves">The moves, as "g1f3".</param>
        /// <param name="maxMoves">How many moves to render before truncating the line.</param>
        public static string ToNotation(string fen, IEnumerable<string> uciMoves, int maxMoves)
        {
            ArgumentNullException.ThrowIfNull(fen);

            if (uciMoves is null) { return ""; }

            ChessBoard board;

            try
            {
                board = ChessBoard.LoadFromFen(fen);
            }
            catch (Exception)
            {
                // A position the library will not accept cannot be replayed, so there is nothing
                // to say. Returning empty lets the caller fall back to the raw move list.
                return "";
            }

            List<string> parts = new();

            foreach (string uci in uciMoves)
            {
                if (parts.Count == maxMoves) { break; }
                if (!TryPlay(board, uci, out string notation)) { break; }

                parts.Add(notation);
            }

            return string.Join(' ', parts);
        }

        private static bool TryPlay(ChessBoard board, string uci, out string notation)
        {
            notation = "";

            if (string.IsNullOrEmpty(uci) || uci.Length < 4) { return false; }

            try
            {
                Piece? moving = board[uci[..2]];

                // The library will play a move for either side, so the turn has to be checked
                // here. Without it a variation belonging to a different position gets replayed
                // as nonsense - and silently, because such a move comes back with no notation.
                if (moving is null || moving.Color != board.Turn) { return false; }

                char? promotion = uci.Length > 4 ? char.ToUpperInvariant(uci[4]) : null;

                Move move = new(uci[..2], uci[2..4]);

                // The notation is produced as the move is played: the board is what knows whether
                // two pieces could reach the square, and whether this move gives check or mate.
                // Move.San is empty until then.
                board.Move(move);

                if (string.IsNullOrEmpty(move.San)) { return false; }

                notation = ToGlyphNotation(move.San, moving, promotion);
                return true;
            }
            catch (Exception)
            {
                // The engine offered a move this board will not accept - most likely the position
                // differs from the one the engine saw. Stop rather than show nonsense.
                return false;
            }
        }

        /// <summary>
        /// Swaps the piece letter for its symbol, in the colour of the piece that moved.
        /// </summary>
        /// <remarks>
        /// Piece letters only: a pawn move is bare ("e4") and belongs that way, and castling is
        /// "O-O" with no piece to show.
        /// </remarks>
        internal static string ToGlyphNotation(string san, Piece? moving, char? promotion)
        {
            if (string.IsNullOrEmpty(san)) { return ""; }

            Dictionary<char, char> glyphs = moving is not null && moving.Color == PieceColor.Black
                ? BlackGlyphs
                : WhiteGlyphs;

            string notation = san;

            if (glyphs.TryGetValue(notation[0], out char pieceGlyph))
            {
                notation = string.Concat(pieceGlyph, notation[1..]);
            }

            // The library promotes to a queen by default and does not always mark it in the SAN,
            // so the promotion is taken from the move itself and added if it is not already there.
            if (promotion is not null && !notation.Contains('=', StringComparison.Ordinal)
                && glyphs.TryGetValue(promotion.Value, out char promotionGlyph))
            {
                notation = $"{notation}={promotionGlyph}";
            }

            return notation;
        }
    }
}
