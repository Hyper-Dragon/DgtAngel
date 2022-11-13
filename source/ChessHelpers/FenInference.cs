namespace ChessHelpers
{
    internal class FenInference
    {
        private readonly static char[] colIdx = "abcdefgh".ToCharArray();
        private readonly static char[] rowIdx = "87654321".ToCharArray();
        private const string halfMoveClock = "0";
        private const string fullMoveNumber = "1";
        private const int squaresOnBoard = 8;

        public static (int diffCount, string squareFrom, string squareTo, string fullFromFen, string promotesTo) SquareDiff(string fromBoardShortFen, string toBoardShortFen)
        {
            //Remove anything after the board position incase it was passed in
            char[] fromBoard = FenInference.FenToCharArray(fromBoardShortFen.Split(' ')[0]);
            char[] toBoard = FenInference.FenToCharArray(toBoardShortFen.Split(' ')[0]);

            bool isWhiteToPlay = false;
            string squareFrom = "";
            string squareTo = "";
            string enPass = "-";
            string promotesTo;
            int squareDiffCount = 0;

            //Enpass test
            foreach (char colourtest in "WB".ToCharArray())
            {
                int moveToRow = colourtest == 'W' ? 2 : 5;
                int moveFromRow = colourtest == 'W' ? 3 : 4;
                char movingPiece = colourtest == 'W' ? 'P' : 'p';
                char oppPiece = colourtest == 'W' ? 'p' : 'P';

                for (int col = 0; col < 8; col++)
                {
                    int colLeft = col - 1;
                    int colRight = col + 1;
                    int toIdx = moveToRow * squaresOnBoard + col;
                    int fromIdx = moveFromRow * squaresOnBoard + col;
                    int colLeftIdx = moveFromRow * squaresOnBoard + colLeft;
                    int colRightIdx = moveFromRow * squaresOnBoard + colRight;

                    if (toBoard[toIdx] == movingPiece &&
                        fromBoard[fromIdx] == oppPiece &&
                        (((colLeft > 0) && ((fromBoard[colLeftIdx] == movingPiece))) ||
                         ((colRight < squaresOnBoard) && ((fromBoard[colRightIdx] == movingPiece)))))
                    {
                        enPass = $"{colIdx[col]}{rowIdx[moveToRow]}";

                        if (colLeft > 0 &&
                            fromBoard[colLeftIdx] == movingPiece &&
                            toBoard[colLeftIdx] == '-')
                        {
                            squareFrom = $"{colIdx[colLeft]}{rowIdx[moveFromRow]}";
                        }
                        else if (colRight < squaresOnBoard &&
                                fromBoard[colRightIdx] == movingPiece &&
                                toBoard[colRightIdx] == '-')
                        {
                            squareFrom = $"{colIdx[colRight]}{rowIdx[moveFromRow]}";
                        }

                        squareTo = $"{colIdx[col]}{rowIdx[moveToRow]}";
                        isWhiteToPlay = char.IsUpper(toBoard[toIdx]);
                        break;
                    }
                }
            }

            string tmpPromotesTo = "";
            bool isPawnMove = false;

            // Even if we have enpass calc the diff count
            // just to check for a legal board
            for (int row = 0; row < squaresOnBoard; row++)
            {
                for (int col = 0; col < squaresOnBoard; col++)
                {
                    if (toBoard[row * squaresOnBoard + col] != fromBoard[row * squaresOnBoard + col])
                    {
                        squareDiffCount++;

                        //If no enpass then look for move
                        if (enPass == "-")
                        {
                            string square = $"{colIdx[col]}{rowIdx[row]}";

                            if (toBoard[row * squaresOnBoard + col] == '-')
                            {
                                squareFrom = square;
                                isPawnMove = fromBoard[row * squaresOnBoard + col] == 'P' || fromBoard[row * squaresOnBoard + col] == 'p';
                            }
                            else //Must be the To Square
                            {
                                squareTo = square;
                                isWhiteToPlay = char.IsUpper(toBoard[row * squaresOnBoard + col]);
                                tmpPromotesTo = (isWhiteToPlay && row == 0) || (!isWhiteToPlay && row == 7) ? $"{char.ToUpper(toBoard[row * squaresOnBoard + col])}" : "";
                            }
                        }
                    }
                }
            }

            promotesTo = enPass == "-" && isPawnMove && !string.IsNullOrEmpty(tmpPromotesTo) ? tmpPromotesTo : "";

            string castle = $"{((toBoard[63] == 'R' && toBoard[60] == 'K') ? "K" : "")}" +
                            $"{((toBoard[56] == 'R' && toBoard[60] == 'K') ? "Q" : "")}" +
                            $"{((toBoard[7] == 'r' && toBoard[4] == 'k') ? "k" : "")}" +
                            $"{((toBoard[0] == 'r' && toBoard[4] == 'k') ? "q" : "")}";

            string inferedFenTailFromPosition = $" {((isWhiteToPlay) ? "w" : "b")} {((string.IsNullOrEmpty(castle) ? "-" : castle))} {enPass} {halfMoveClock} {fullMoveNumber}";

            return (squareDiffCount, squareFrom, squareTo, $"{fromBoardShortFen.Split(' ')[0]}{inferedFenTailFromPosition}", promotesTo);
        }


        public static char[] FenToCharArray(string fen, in bool isFlipRequired = false)
        {
            char[] boardArrayOut = "".PadRight(64, '-').ToCharArray();

            if (isFlipRequired)
            {
                string[] fenRows = fen.Split('/');
                Array.Reverse(fenRows);
                fen = "";

                foreach (string row in fenRows)
                {
                    char[] fenRowReversed = row.ToCharArray();
                    Array.Reverse(fenRowReversed);
                    fen += new string(fenRowReversed);
                }
            }

            int convIdx = 0;
            foreach (char squareValue in fen)
            {
                if ("rnbqkpPRNBQK".Contains(squareValue))
                {
                    boardArrayOut[convIdx++] = squareValue;
                }
                else if ("12345678".Contains(squareValue))
                {
                    convIdx += int.Parse(squareValue.ToString());
                }
            }

            return boardArrayOut;
        }

    }
}
