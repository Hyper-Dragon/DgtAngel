using System;
using System.Collections.Generic;
using System.Text;

namespace ChessHelpersTest.TestSupport
{
    /// <summary>
    /// Deterministic synthetic board generation. No randomised wall-clock seeding
    /// anywhere - every board is reproducible from a seed, so a failure can be replayed.
    /// </summary>
    public static class SyntheticBoards
    {
        private static readonly char[] WhitePieces = { 'K', 'Q', 'R', 'B', 'N', 'P' };
        private static readonly char[] BlackPieces = { 'k', 'q', 'r', 'b', 'n', 'p' };

        /// <summary>
        /// The DGT driver's own board format: pieces plus decimal blank runs, with no
        /// rank separators and runs allowed to span ranks (so "32" is one number,
        /// not three then two).
        /// </summary>
        public static string ToDriverString(char[] flat)
        {
            ArgumentNullException.ThrowIfNull(flat);
            if (flat.Length != 64)
            {
                throw new ArgumentException($"expected 64 squares, got {flat.Length}", nameof(flat));
            }

            StringBuilder builder = new();
            int blanks = 0;

            foreach (char square in flat)
            {
                if (square == '-')
                {
                    blanks++;
                    continue;
                }

                if (blanks > 0)
                {
                    _ = builder.Append(blanks);
                    blanks = 0;
                }

                _ = builder.Append(square);
            }

            if (blanks > 0)
            {
                _ = builder.Append(blanks);
            }

            return builder.ToString();
        }

        /// <summary>
        /// A board with both kings plus <paramref name="extraPieces"/> other pieces on
        /// distinct squares, chosen by a seeded generator.
        /// </summary>
        public static char[] RandomBoard(int seed, int extraPieces)
        {
            if (extraPieces < 0 || extraPieces > 30)
            {
                throw new ArgumentOutOfRangeException(nameof(extraPieces), extraPieces, "0..30");
            }

            char[] flat = new char[64];
            Array.Fill(flat, '-');

            Random random = new(seed);

            List<int> free = new(64);
            for (int i = 0; i < 64; i++)
            {
                free.Add(i);
            }

            int TakeSquare()
            {
                int pick = random.Next(free.Count);
                int square = free[pick];
                free.RemoveAt(pick);
                return square;
            }

            flat[TakeSquare()] = 'K';
            flat[TakeSquare()] = 'k';

            for (int i = 0; i < extraPieces; i++)
            {
                char[] bank = i % 2 == 0 ? WhitePieces : BlackPieces;
                flat[TakeSquare()] = bank[random.Next(bank.Length)];
            }

            return flat;
        }

        /// <summary>
        /// Every move from an occupied square to an empty one, with the resulting board.
        /// These are the unambiguous two-square diffs.
        /// </summary>
        public static IEnumerable<(string from, string to, string resultFen)> QuietMoves(string boardFen)
        {
            char[] flat = FenBoard.Parse(boardFen);

            for (int from = 0; from < 64; from++)
            {
                if (flat[from] == '-')
                {
                    continue;
                }

                for (int to = 0; to < 64; to++)
                {
                    if (flat[to] != '-')
                    {
                        continue;
                    }

                    char[] moved = (char[])flat.Clone();
                    moved[to] = moved[from];
                    moved[from] = '-';

                    yield return (FenBoard.Square(from), FenBoard.Square(to), FenBoard.Format(moved));
                }
            }
        }

        /// <summary>
        /// Every move from an occupied square onto an opposite-coloured piece, with the
        /// resulting board. Also a two-square diff, but one square changes piece rather
        /// than emptying.
        /// </summary>
        public static IEnumerable<(string from, string to, string resultFen)> Captures(string boardFen)
        {
            char[] flat = FenBoard.Parse(boardFen);

            for (int from = 0; from < 64; from++)
            {
                char piece = flat[from];
                if (piece == '-')
                {
                    continue;
                }

                bool movingIsWhite = char.IsUpper(piece);

                for (int to = 0; to < 64; to++)
                {
                    char target = flat[to];
                    if (target == '-' || char.IsUpper(target) == movingIsWhite)
                    {
                        continue;
                    }

                    char[] moved = (char[])flat.Clone();
                    moved[to] = moved[from];
                    moved[from] = '-';

                    yield return (FenBoard.Square(from), FenBoard.Square(to), FenBoard.Format(moved));
                }
            }
        }

        /// <summary>
        /// En-passant capture of a pawn that has just advanced two squares, given as
        /// board-before and board-after. <paramref name="destinationFile"/> is the file
        /// the capturing pawn lands on (0 = a, 7 = h).
        /// </summary>
        public static (string before, string after, string from, string to) EnPassant(bool whiteCaptures, int destinationFile)
        {
            if (destinationFile is < 0 or > 7)
            {
                throw new ArgumentOutOfRangeException(nameof(destinationFile), destinationFile, "0..7");
            }

            char[] before = new char[64];
            char[] after = new char[64];
            Array.Fill(before, '-');
            Array.Fill(after, '-');

            char moving = whiteCaptures ? 'P' : 'p';
            char captured = whiteCaptures ? 'p' : 'P';

            // The captured pawn sits on the destination file, one rank behind where the
            // capturing pawn lands. The capturing pawn starts beside it.
            int capturedRank = whiteCaptures ? 5 : 4;
            int destinationRank = whiteCaptures ? 6 : 3;

            int capturedSquare = FenBoard.Index($"{FileChar(destinationFile)}{capturedRank}");
            int destination = FenBoard.Index($"{FileChar(destinationFile)}{destinationRank}");

            // Prefer the file to the left of the destination; fall back to the right when
            // the destination is the a-file.
            int originFile = destinationFile > 0 ? destinationFile - 1 : destinationFile + 1;
            int origin = FenBoard.Index($"{FileChar(originFile)}{capturedRank}");

            before[capturedSquare] = captured;
            before[origin] = moving;

            after[destination] = moving;

            return (
                FenBoard.Format(before),
                FenBoard.Format(after),
                FenBoard.Square(origin),
                FenBoard.Square(destination));
        }

        private static char FileChar(int file) => (char)('a' + file);
    }
}
