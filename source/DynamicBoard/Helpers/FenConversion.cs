namespace DynamicBoard.Helpers
{
    public class FenConversion
    {
        public static int SquareDiffCount(string localBoardFen, string remoteBoardFen)
        {
            char[] board1 = FenConversion.FenToCharArray(remoteBoardFen);
            char[] board2 = FenConversion.FenToCharArray(localBoardFen);

            //Count the differences between board1 and board2
            int diff = 0;
            for (int i = 0; i < board1.Length; i++)
            {
                if (board1[i] != board2[i])
                {
                    diff++;
                }
            }

            return diff;
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
