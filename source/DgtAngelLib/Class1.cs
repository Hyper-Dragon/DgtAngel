using ChessDotNet.Pieces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DgtAngelLib
{
    public class ChessDotComHelpers
    {
        //public string ConvertClockToTimespan(string htmlIn)
        //{
        //
        //}


        /// <summary>
        /// Turns the output of document.getElementsByClassName('piece')
        /// into a FEN string
        /// </summary>
        /// <param name="htmlIn">
        ///Example String
        ///[div.piece.bk.square-66, div.piece.br.square-36, div.piece.bp.square-84,
        ///div.piece.bp.square-45, div.piece.bp.square-26, div.piece.wp.square-72,
        ///div.piece.wp.square-44, div.piece.wp.square-33, div.piece.wp.square-12,
        ///div.piece.wk.square-62, div.piece.wr.square-86] 
        /// </param>
        /// <returns>Fen String</returns>
        public string ConvertHtmlToFen(string htmlIn)
        {
            ChessDotNet.Piece[][] board = new ChessDotNet.Piece[8][];
            for (int loop = 0; loop < 8; loop++)
            {
                board[loop] = new ChessDotNet.Piece[8];
            }

            foreach (string sqr in htmlIn[1..^1].Replace("div.piece.", "").Replace(".square-", "").Replace(" ", "").Split(','))
            {
                board[7 - (int.Parse(sqr[3].ToString()) - 1)][int.Parse(sqr[2].ToString()) - 1] = sqr[1] switch
                {
                    'p' => new ChessDotNet.Pieces.Pawn(((sqr[0] == 'w') ? ChessDotNet.Player.White : ChessDotNet.Player.Black)),
                    'n' => new ChessDotNet.Pieces.Knight(((sqr[0] == 'w') ? ChessDotNet.Player.White : ChessDotNet.Player.Black)),
                    'b' => new ChessDotNet.Pieces.Bishop((sqr[0] == 'w') ? ChessDotNet.Player.White : ChessDotNet.Player.Black),
                    'r' => new ChessDotNet.Pieces.Rook(((sqr[0] == 'w') ? ChessDotNet.Player.White : ChessDotNet.Player.Black)),
                    'k' => new ChessDotNet.Pieces.King(((sqr[0] == 'w') ? ChessDotNet.Player.White : ChessDotNet.Player.Black)),
                    'q' => new ChessDotNet.Pieces.Queen(((sqr[0] == 'w') ? ChessDotNet.Player.White : ChessDotNet.Player.Black)),
                    _ => throw new Exception("Unknown Piece Type")
                };
            }

            return new ChessDotNet.ChessGame(new ChessDotNet.GameCreationData()
            {
                WhoseTurn = ChessDotNet.Player.White,
                Board = board,
            }).GetFen();
        }
    }
}
