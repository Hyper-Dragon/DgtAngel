using Chess;
using System.Globalization;
using System.Text;

namespace ChessHelpers
{
    public static class Extensions
    {
        public static string GenrateAlwaysValidMovesFEN(this string shortFenIn, bool isPlayingWhite)
        {
            //if(!isPlayingWhite) { return shortFenIn; }

            // The side is the opposite of the players...
            string fullFen = $"{shortFenIn} {(isPlayingWhite ? "w" : "b")} - - 0 10";

            // Create a board
            ChessBoard board = ChessBoard.LoadFromFen(fullFen);

            char[] clearedflatFenArray = FenInference.
                                         FenToCharArray(shortFenIn).
                                         Select(c => (isPlayingWhite ? char.IsUpper(c) : char.IsLower(c)) ? c : '-').
                                         ToArray<char>();

            //if ((board.IsEndGame && board.EndGame != null && board.EndGame.WonSide == null) ||
            //         (isPlayingWhite && board.BlackKingChecked) ||
            //         (!isPlayingWhite && board.WhiteKingChecked))
            //{
            for (int sq = isPlayingWhite ? 0 : clearedflatFenArray.Length - 1;
                (isPlayingWhite && sq < clearedflatFenArray.Length) ||
                (!isPlayingWhite && sq >= 0);
                sq = isPlayingWhite ? sq + 1 : sq - 1)
            {
                if (clearedflatFenArray[sq] == '-')
                {
                    clearedflatFenArray[sq] = isPlayingWhite ? 'k' : 'K';
                    string candidateFen = clearedflatFenArray.ConvertFlatArrayToFen($" {(isPlayingWhite ? "w" : "b")} - - 0 10");

                    ChessBoard testBoard = ChessBoard.LoadFromFen(candidateFen);

                    if ((testBoard.IsEndGame && testBoard.EndGame != null && testBoard.EndGame.WonSide == null) ||
                         (isPlayingWhite && testBoard.BlackKingChecked) ||
                         (!isPlayingWhite && testBoard.WhiteKingChecked))
                    {
                        clearedflatFenArray[sq] = '-';
                    }
                    else
                    {
                        return candidateFen.Split(" ")[0];
                    }
                }
            }
            //}

            //If we hit this no suitable position was found so just send out 
            //the normal FEN for now
            //TODO: We could try adding additional pieces to the board just in case
            //      there is a position where the king can't be placed anywhere!
            return shortFenIn;
        }

        public static string ConvertFlatArrayToFen(this char[] flatFenArray, string tail = "")
        {
            ArgumentNullException.ThrowIfNull(flatFenArray);

            StringBuilder fenFromArrayBuilder = new();

            for (int sq = 0, blankTot = 0, file = 0; sq < flatFenArray.Length; sq++)
            {

                if (flatFenArray[sq] == '-')
                {
                    blankTot++;
                }
                else
                {
                    _ = fenFromArrayBuilder.Append(CultureInfo.InvariantCulture, $"{(blankTot > 0 ? blankTot : "")}{flatFenArray[sq]}");
                    blankTot = 0;

                }

                if (++file == 8)
                {
                    _ = fenFromArrayBuilder.Append(CultureInfo.InvariantCulture, $"{(blankTot > 0 ? blankTot : "")}{(sq < (flatFenArray.Length - 8) ? @"/" : "")}");
                    blankTot = 0;
                    file = 0;
                }
            }

            return fenFromArrayBuilder.Append(tail).ToString();

        }

        /// <summary>
        /// The DGT driver reports the sensor board as a run-length encoded string with no
        /// rank separators.  Two shapes have been seen in the wild:
        ///   * rank aligned  - "88888888", where every rank still comes to 8 squares;
        ///   * cross rank    - "rnbqkbnrpppppppp32PPPPPPPP1NBQKBNR", where a single run
        ///                     ("32") covers ranks 5 down to 2.
        /// Every consumer downstream expects a real FEN board field: the fake LiveChess
        /// feed on :1982 and the gRPC clients on :37965.
        /// </summary>
        /// <returns>
        /// The FEN board field, or the original string when it is empty, already a FEN, or
        /// does not describe exactly 64 squares (a board mid-scan must not be reported as a
        /// position full of empty squares).
        /// </returns>
        public static string NormaliseDgtBoardToFen(this string boardIn)
        {
            if (string.IsNullOrWhiteSpace(boardIn))
            {
                return boardIn;
            }

            string board = boardIn.Trim().Split(' ')[0];

            //Already a FEN board field - nothing to do.
            if (board.Contains('/', StringComparison.Ordinal))
            {
                return board;
            }

            char[]? squares = TryDecodeRankAligned(board) ?? TryDecodeRunsAcrossRanks(board);

            return squares == null ? board : squares.ConvertFlatArrayToFen();
        }

        /// <summary>
        /// Rank aligned form - every run has to fit inside its rank.
        /// </summary>
        private static char[]? TryDecodeRankAligned(string board)
        {
            char[] squares = "".PadRight(64, '-').ToCharArray();
            int index = 0;

            foreach (char square in board)
            {
                if (square is >= '1' and <= '8')
                {
                    int run = square - '0';

                    //A run that straddles a rank boundary means this is not the rank
                    //aligned shape.
                    if ((index % 8) + run > 8)
                    {
                        return null;
                    }

                    index += run;
                }
                else if ("rnbqkpRNBQKP".Contains(square, StringComparison.Ordinal))
                {
                    squares[index++] = square;
                }
                else
                {
                    return null;
                }

                if (index > 64)
                {
                    return null;
                }
            }

            return index == 64 ? squares : null;
        }

        /// <summary>
        /// The driver's own form, where a run is a number of empty squares and can cross
        /// rank boundaries - consecutive digits are one number ("32", not 3 then 2).
        /// </summary>
        private static char[]? TryDecodeRunsAcrossRanks(string board)
        {
            char[] squares = "".PadRight(64, '-').ToCharArray();
            int index = 0;
            int runValue = 0;
            bool pendingRun = false;

            foreach (char square in board)
            {
                if (square is >= '0' and <= '9')
                {
                    runValue = (runValue * 10) + (square - '0');
                    pendingRun = true;
                    continue;
                }

                if (!"rnbqkpRNBQKP".Contains(square, StringComparison.Ordinal))
                {
                    return null;
                }

                if (pendingRun)
                {
                    index += runValue;
                    runValue = 0;
                    pendingRun = false;

                    if (index > 64)
                    {
                        return null;
                    }
                }

                squares[index++] = square;

                if (index > 64)
                {
                    return null;
                }
            }

            if (pendingRun)
            {
                index += runValue;
            }

            return index == 64 ? squares : null;
        }
    }
}
