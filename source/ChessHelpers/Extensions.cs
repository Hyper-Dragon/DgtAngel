using Chess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessHelpers
{
    public static class Extensions
    {
        public static string GenrateAlwaysValidMovesFEN(this string shortFenIn, bool isPlayingWhite)
        {
            // The side is the opposite of the players...
            string fullFen = $"{shortFenIn} {(isPlayingWhite?"w":"b")} - - 0 10";

            // Create a board
            ChessBoard board = ChessBoard.LoadFromFen(fullFen);

            if ((board.IsEndGame && board.EndGame != null && board.EndGame.WonSide == null) ||
               (isPlayingWhite && board.BlackKingChecked)  ||
               (!isPlayingWhite && board.WhiteKingChecked) )
            {
                var clearedflatFenArray = FenInference.
                                          FenToCharArray(shortFenIn).
                                          Select(c => (isPlayingWhite ? char.IsUpper(c) : char.IsLower(c)) ? c : '-').
                                          ToArray<char>();

                
                for (int sq = 0; sq < clearedflatFenArray.Length; sq++)
                {
                    if (clearedflatFenArray[sq] == '-')
                    {
                        clearedflatFenArray[sq] = isPlayingWhite ? 'k' : 'K';
                        string candidateFen = clearedflatFenArray.ConvertFlatArrayToFen($" {(isPlayingWhite ? "w" : "b")} - - 0 10");

                        var testBoard = ChessBoard.LoadFromFen(candidateFen);

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
                    fenFromArrayBuilder.Append($"{(blankTot > 0 ? blankTot : "")}{flatFenArray[sq]}");
                    blankTot = 0;

                }

                if (++file == 8)
                {
                    fenFromArrayBuilder.Append($"{(blankTot > 0 ? blankTot : "")}{(sq < (flatFenArray.Length - 8) ? @"/" : "")}");
                    blankTot = 0;
                    file = 0;
                }
            }

            return fenFromArrayBuilder.Append(tail).ToString();

        }
    }
}
