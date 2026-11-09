using System;
using System.Text;

namespace DynamicBoardTest.TestSupport
{
    /// <summary>
    /// Independent board-FEN parsing used as an oracle.
    ///
    /// Deliberately does not call DynamicBoard.Helpers.FenConversion or ChessHelpers -
    /// an oracle built on the code under test confirms whatever that code does.
    /// Index convention: 0 = a8, 63 = h1.
    /// </summary>
    public static class FenBoard
    {
        public const string Start = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";
        public const string Empty = "8/8/8/8/8/8/8/8";

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
                        for (int i = 0; i < c - '0'; i++)
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

        public static int Index(string square)
        {
            ArgumentNullException.ThrowIfNull(square);
            int file = square[0] - 'a';
            int rank = square[1] - '0';
            if (square.Length != 2 || file is < 0 or > 7 || rank is < 1 or > 8)
            {
                throw new ArgumentException($"not an algebraic square: '{square}'", nameof(square));
            }

            return ((8 - rank) * 8) + file;
        }

        public static string Square(int index)
        {
            if (index is < 0 or > 63)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "outside 0..63");
            }

            return $"{(char)('a' + (index % 8))}{(char)('0' + (8 - (index / 8)))}";
        }

        public static string Move(string boardFen, string from, string to)
        {
            char[] flat = Parse(boardFen);
            int fromIdx = Index(from);
            int toIdx = Index(to);

            if (flat[fromIdx] == '-')
            {
                throw new ArgumentException($"no piece on {from} in '{boardFen}'", nameof(from));
            }

            flat[toIdx] = flat[fromIdx];
            flat[fromIdx] = '-';
            return Format(flat);
        }

        /// <summary>A board with both kings plus pieces taken from a seeded generator.</summary>
        public static char[] RandomBoard(int seed, int extraPieces)
        {
            char[] flat = new char[64];
            Array.Fill(flat, '-');

            Random random = new(seed);
            char[] bank = { 'K', 'Q', 'R', 'B', 'N', 'P', 'k', 'q', 'r', 'b', 'n', 'p' };

            for (int placed = 0; placed < 2 + extraPieces; placed++)
            {
                int square = random.Next(64);
                while (flat[square] != '-')
                {
                    square = (square + 1) % 64;
                }

                flat[square] = placed == 0 ? 'K' : placed == 1 ? 'k' : bank[random.Next(bank.Length)];
            }

            return flat;
        }
    }
}
