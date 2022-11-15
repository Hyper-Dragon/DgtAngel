﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Chess;

namespace ChessHelpers
{
    public static class PositionDiffCalculator
    {
        public static (string move, string ending) CalculateSanFromFen(string fromFen, string toFen)
        {
            if (string.IsNullOrWhiteSpace(fromFen)) { return (" ",""); }

            try
            {
                var (diffCount, squareFrom, squareTo, fullFromFen, promotesTo) = FenInference.SquareDiff(fromFen, toFen);

                var board = ChessBoard.LoadFromFen(fullFromFen);
                board.Move(new Move(squareFrom, squareTo));

                string move = board.MovesToSan.FirstOrDefault<string>("");

                //HACK: the library returns ?x?x?? for enpassant - fix to ?x?x??
                if(move.Length==6 && move[1]=='x' && move[3]=='x') { move = move.Substring(2);  }


                string ending = ((!board.IsEndGame) ? 
                                  "": ((board.EndGame != null && board.EndGame.WonSide == null) ? 
                                        "1/2-1/2" : ((board.EndGame != null && board.EndGame.WonSide == PieceColor.White) ?
                                                      "1-0" : "0-1")));
                
                return (move,ending);
            }
            catch (Exception) { 
                //This is missing step betweem 2 FENS so no move to return
                return (" ",""); 
            }
        }

    }
}