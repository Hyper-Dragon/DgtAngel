using System;
using System.Text;

namespace ChessHelpersTest.TestSupport
{
    /// <summary>
    /// Builds and inspects board-only FEN strings (the eight rank fields, no side to
    /// move, castling, en passant or move counters).
    /// </summary>
    /// <remarks>
    /// This is deliberately an INDEPENDENT implementation of FEN parsing. It must never
    /// call into ChessHelpers: an oracle that reuses the code under test will happily
    /// confirm whatever that code happens to do, bugs included.
    ///
    /// Index convention matches the production code: 0 = a8, 7 = h8, 56 = a1, 63 = h1.
    /// </remarks>
    public static class FenBoard
    {
        public const string Start = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";
        public const string Empty = "8/8/8/8/8/8/8/8";

        /// <summary>Expands a board-only FEN into a 64-character flat array ('-' = empty).</summary>
        public static char[] Parse(string boardFen)
        {
            ArgumentNullException.ThrowIfNull(boardFen);

            string[] ranks = boardFen.Split('/');
            if (ranks.Length != 8)
            {
                throw new ArgumentException($"expected 8 ranks, got {ranks.Length}: '{boardFen}'", nameof(boardFen));
            }

            char[] flat = new char[64];
            for (int rank = 0; rank < 8; rank++)
            {
                int file = 0;
                foreach (char c in ranks[rank])
                {
                    if (c is >= '1' and <= '8')
                    {
                        int blanks = c - '0';
                        for (int i = 0; i < blanks; i++)
                        {
                            flat[(rank * 8) + file++] = '-';
                        }
                    }
                    else
                    {
                        flat[(rank * 8) + file++] = c;
                    }
                }

                if (file != 8)
                {
                    throw new ArgumentException($"rank {rank} describes {file} squares: '{boardFen}'", nameof(boardFen));
                }
            }

            return flat;
        }

        /// <summary>Collapses a 64-character flat array back into a board-only FEN.</summary>
        public static string Format(char[] flat)
        {
            ArgumentNullException.ThrowIfNull(flat);
            if (flat.Length != 64)
            {
                throw new ArgumentException($"expected 64 squares, got {flat.Length}", nameof(flat));
            }

            StringBuilder builder = new();
            for (int rank = 0; rank < 8; rank++)
            {
                int blanks = 0;
                for (int file = 0; file < 8; file++)
                {
                    char c = flat[(rank * 8) + file];
                    if (c == '-')
                    {
                        blanks++;
                        continue;
                    }

                    if (blanks > 0)
                    {
                        _ = builder.Append(blanks);
                        blanks = 0;
                    }

                    _ = builder.Append(c);
                }

                if (blanks > 0)
                {
                    _ = builder.Append(blanks);
                }

                if (rank < 7)
                {
                    _ = builder.Append('/');
                }
            }

            return builder.ToString();
        }

        /// <summary>Flat index for an algebraic square, e.g. "a8" = 0, "h1" = 63.</summary>
        public static int Index(string square)
        {
            ArgumentNullException.ThrowIfNull(square);
            if (square.Length != 2)
            {
                throw new ArgumentException($"not an algebraic square: '{square}'", nameof(square));
            }

            int file = square[0] - 'a';
            int rank = square[1] - '0';
            if (file is < 0 or > 7 || rank is < 1 or > 8)
            {
                throw new ArgumentException($"not an algebraic square: '{square}'", nameof(square));
            }

            return ((8 - rank) * 8) + file;
        }

        /// <summary>Algebraic square for a flat index, e.g. 0 = "a8", 63 = "h1".</summary>
        public static string Square(int index)
        {
            if (index is < 0 or > 63)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "outside 0..63");
            }

            char file = (char)('a' + (index % 8));
            char rank = (char)('0' + (8 - (index / 8)));
            return $"{file}{rank}";
        }

        /// <summary>Applies a move to a board and returns the resulting board FEN.</summary>
        public static string Move(string boardFen, string from, string to)
        {
            char[] flat = Parse(boardFen);
            int fromIdx = Index(from);
            int toIdx = Index(to);

            char piece = flat[fromIdx];
            if (piece == '-')
            {
                throw new ArgumentException($"no piece on {from} in '{boardFen}'", nameof(from));
            }

            flat[fromIdx] = '-';
            flat[toIdx] = piece;
            return Format(flat);
        }

        /// <summary>Every occupied square, as algebraic names, in board order.</summary>
        public static System.Collections.Generic.List<(string square, char piece)> Occupied(string boardFen)
        {
            char[] flat = Parse(boardFen);
            System.Collections.Generic.List<(string square, char piece)> result = new();

            for (int i = 0; i < flat.Length; i++)
            {
                if (flat[i] != '-')
                {
                    result.Add((Square(i), flat[i]));
                }
            }

            return result;
        }
    }
}
