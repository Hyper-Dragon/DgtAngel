using System;
using System.Collections.Concurrent;
using System.Text;
using UnityEngine;

public class MessageHandler : MonoBehaviour
{
    private string currentFen = "8/8/8/8/8/8/8/8";
    private char[] fenConversionCurrent;

    public static ConcurrentQueue<string> SquareDiffQueue = new();
    
    public static string WhiteClock { get; private set; } = "0:00:00";
    public static string BlackClock { get; private set; } = "0:00:00";
    public static string MessageText { get; private set; } = "---";
    public static bool IsRemoteWhiteOnBottom { get; private set; } = true;

    /// <summary>
    /// Set the board orientation 
    /// </summary>
    /// <param name="isWhiteOnBottom"></param>
    public void WhiteOrientation(string isWhiteOnBottom)
    {
        IsRemoteWhiteOnBottom = bool.Parse(isWhiteOnBottom);
    }
    
    /// <summary>
    /// Set the board side messages
    /// </summary>
    /// <param name="text"></param>
    public void SetText(string text)
    {
        MessageText = text;
    }

    /// <summary>
    /// Set the clock times
    /// </summary>
    /// <param name="clocks">Clock times in ms split with '/'</param>
    public void SetClocks(string clocks)
    {
        string[] clockSplit = clocks.Split('/');

        var whiteClockTime = TimeSpan.FromMilliseconds((long.Parse(clockSplit[0])));
        var blackClockTime = TimeSpan.FromMilliseconds((long.Parse(clockSplit[1])));

        WhiteClock = whiteClockTime.ToString(@"h\:mm\:ss");
        BlackClock = blackClockTime.ToString(@"h\:mm\:ss");
    }


    public void ResetFen(string fenIn)
    {
        UpdatedFenInternal(fenIn,true);
        UpdatedFenInternal(fenIn, true);
    }

    public void UpdatedFen(string fenIn)
    {
        UpdatedFenInternal(fenIn);
    }

    /// <summary>
    /// Board position update
    /// </summary>
    /// <param name="fenIn">Fen string - board only (exclude the turn no, castling etc)</param>
    private void UpdatedFenInternal(string fenIn, bool isNoMove = false)
    {
        //print($"FEN in:: {fenIn}");

        //Update highlighs only if we have a previous board to compare
        if (!string.IsNullOrEmpty(currentFen))
        {
            if (isNoMove || currentFen != fenIn)
            {
                StringBuilder highlights = new();
                char[] fenConversionIn = FenConversion.FenToCharArray(fenIn);

                for (int row = 7, posCount = 0; row >= 0; row--)
                {
                    for (int col = 7; col >= 0; col--, posCount++)
                    {
                        //...highlighting differences...
                        if (fenConversionCurrent[posCount] != fenConversionIn[posCount])
                        {
                            highlights.Append($"{(highlights.Length == 0 ? "" : ",")}{(char)('A' + (7 - col))}{row + 1}");
                        }
                    }
                }

                //print($"DIFF::{HighlightSquares}");
                MessageHandler.SquareDiffQueue.Enqueue(highlights.ToString());

                currentFen = fenIn;
                fenConversionCurrent = fenConversionIn;
            }
        }
        else
        {
            currentFen = fenIn;
            fenConversionCurrent = FenConversion.FenToCharArray(currentFen);
        }
    }
}


internal class FenConversion
{
    internal static char[] FenToCharArray(string fen)
    {
        char[] boardArrayOut =  "".PadRight(64, '-').ToCharArray();
        int convIdx = 0;

        Array.ForEach(fen.ToCharArray(), 
                      squareValue => {
                          if ("rnbqkpPRNBQK".Contains(squareValue))
                          {
                              boardArrayOut[convIdx++] = squareValue;
                          }
                          else if ("12345678".Contains(squareValue))
                          {
                              convIdx += int.Parse(squareValue.ToString());
                          }
                      });

        return boardArrayOut;
    }
}

