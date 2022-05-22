using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public class MessageHandler : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void HelloString(string str);

    public static string WhiteClock { get; private set; } = "0:00:00";
    public static string BlackClock { get; private set; } = "0:00:00";
    public static string MessageText { get; private set; } = "---";

    public static bool IsRemoteWhiteOnBottom { get; private set; } = true;
    public static ConcurrentQueue<string> SquareDiffQueue = new();
    
    private string currentFen = "8/8/8/8/8/8/8/8";
    public string HighlightSquares { get; set; } = "";

    [SerializeField] private string fenString;

    public void WhiteOrientation(string isWhiteOnBottom)
    {
        IsRemoteWhiteOnBottom = bool.Parse(isWhiteOnBottom);
    }

    public void SetClocks(string clocks)
    {
        string[] clockSplit = clocks.Split('/');

        var whiteClockTime = TimeSpan.FromMilliseconds((long.Parse(clockSplit[0])));
        var blackClockTime = TimeSpan.FromMilliseconds((long.Parse(clockSplit[1])));

        WhiteClock = whiteClockTime.ToString(@"h\:mm\:ss");
        BlackClock = blackClockTime.ToString(@"h\:mm\:ss");
    }

    public void SetText(string text)
    {
        MessageText = text;
    }

    public void UpdatedFen(string fenIn)
    {
        print($"FEN in:: {fenIn}");

        if (!string.IsNullOrEmpty(currentFen))
        {
            if (currentFen != fenIn)
            {
                StringBuilder highlights = new ();

                char[] fenConversionCurrent = FenConversion.FenToCharArray(currentFen);
                char[] fenConversionIn = FenConversion.FenToCharArray(fenIn);

                for (int row = 7, posCount = 0; row >= 0; row--)
                {
                    for (int col = 7; col >= 0; col--, posCount++)
                    {
                        //...highlighting differences...
                        if (fenConversionCurrent[posCount] != fenConversionIn[posCount])
                        {
                            // SET LIGHT COMMANDS
                            highlights.Append($"{(highlights.Length == 0 ? "" : ",")}{((char)('A' + (7 - col))).ToString()}{row + 1}");
                        }
                    }
                }

                HighlightSquares = highlights.ToString();
                //print($"DIFF::{HighlightSquares}");


                MessageHandler.SquareDiffQueue.Enqueue(HighlightSquares);
            }
        }

        currentFen = fenIn;
    }


    // Start is called before the first frame update
    void Start()
    {
        //HelloString("This is a string.");
    }

}


internal class FenConversion
{
    internal static char[] FenToCharArray(string fen, in bool isFlipRequired = false)
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

