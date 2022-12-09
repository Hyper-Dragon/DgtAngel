using Chess;

namespace ChessHelpers
{
    public static class PositionDiffCalculator
    {
        public static (string move, string ending, string turn) CalculateSanFromFen(string fromFen, string toFen)
        {
            if (string.IsNullOrWhiteSpace(fromFen)) { return ("", "", ""); }

            try
            {
                (int diffCount, string squareFrom, string squareTo, string fullFromFen, string promotesTo) = FenInference.SquareDiff(fromFen, toFen);

                ChessBoard board = ChessBoard.LoadFromFen(fullFromFen);
                _ = board.Move(new Move(squareFrom, squareTo));

                string move = board.MovesToSan.FirstOrDefault<string>("");

                //HACK: the library returns ?x?x?? for enpassant - fix to ?x?x??
                if (move.Length == 6 && move[1] == 'x' && move[3] == 'x') { move = move[2..]; }

                if (move.Contains("=Q")) { move = move.Replace("=Q", $"={promotesTo.ToUpperInvariant()}"); }

                string ending = (!board.IsEndGame) ?
                                  "" : ((board.EndGame != null && board.EndGame.WonSide == null) ?
                                        "1/2-1/2" : ((board.EndGame != null && board.EndGame.WonSide == PieceColor.White) ?
                                                      "1-0" : "0-1"));

                //Getting the next move so return the opposite
                return (move.Trim(), ending, (board.Turn == PieceColor.White) ? "WHITE" : "BLACK");
            }
            catch (Exception)
            {
                //This is missing step betweem 2 FENS so no move to return
                return ("", "", "");
            }
        }

    }
}
