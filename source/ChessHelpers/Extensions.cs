﻿using Chess;
using System.Text;

namespace ChessHelpers
{
    public static class Extensions
    {
        public static string GenrateAlwaysValidMovesFEN(this string shortFenIn, bool isPlayingWhite)
        {
            // The side is the opposite of the players...
            string fullFen = $"{shortFenIn} {(isPlayingWhite ? "w" : "b")} - - 0 10";

            // Create a board
            ChessBoard board = ChessBoard.LoadFromFen(fullFen);

            if ((board.IsEndGame && board.EndGame != null && board.EndGame.WonSide == null) ||
               (isPlayingWhite && board.BlackKingChecked) ||
               (!isPlayingWhite && board.WhiteKingChecked))
            {
                char[] clearedflatFenArray = FenInference.
                                          FenToCharArray(shortFenIn).
                                          Select(c => (isPlayingWhite ? char.IsUpper(c) : char.IsLower(c)) ? c : '-').
                                          ToArray<char>();


                for (int sq = 0; sq < clearedflatFenArray.Length; sq++)
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
                            return candidateFen;
                        }
                    }
                }

                //If we hit this no suitable position was found so just send out 
                //the normal FEN for now
                //TODO: We could try adding additional pieces to the board just in case
                //      there is a position where the king can't be placed anywhere! 
                return shortFenIn;
            }
            else
            {
                return shortFenIn;
            }
        }

        public static string ConvertFlatArrayToFen(this char[] flatFenArray, string tail = "")
        {
            StringBuilder fenFromArrayBuilder = new();

            for (int sq = 0, blankTot = 0, file = 0; sq < flatFenArray.Length; sq++)
            {

                if (flatFenArray[sq] == '-')
                {
                    blankTot++;
                }
                else
                {
                    _ = fenFromArrayBuilder.Append($"{(blankTot > 0 ? blankTot : "")}{flatFenArray[sq]}");
                    blankTot = 0;

                }

                if (++file == 8)
                {
                    _ = fenFromArrayBuilder.Append($"{(blankTot > 0 ? blankTot : "")}{(sq < (flatFenArray.Length - 8) ? @"/" : "")}");
                    blankTot = 0;
                    file = 0;
                }
            }

            return fenFromArrayBuilder.Append(tail).ToString();

        }
    }
}
