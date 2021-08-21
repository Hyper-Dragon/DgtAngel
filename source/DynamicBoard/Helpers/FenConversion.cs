using System;

namespace DynamicBoard.Helpers
{
    class FenConversion
    {
        internal static char[] FenToCharArray(string fen, bool isFlipRequired = false)
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
