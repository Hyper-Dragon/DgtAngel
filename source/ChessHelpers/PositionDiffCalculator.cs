using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chess;

namespace ChessHelpers
{
    public static class PositionDiffCalculator
    {

        public static string CalculateSanFromFen(string fromFen, string toFen)
        {
            var (diffCount, squareFrom, squareTo, fullFromFen, promotesTo) = FenInference.SquareDiff(fromFen, toFen);

            var board = ChessBoard.LoadFromFen(fullFromFen);
            board.Move(new Move(squareFrom, squareTo));
            string move = board.MovesToSan.FirstOrDefault<string>("");

            return move;
        }

    }
}
