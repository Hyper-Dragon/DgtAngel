using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;

public class NetClient : MonoBehaviour
{
    public static NetClient instance;
    public string AddrBase = @"http://localhost:37964/CherubVirtualClock/GetStuff/";

    private StreamReader streamRead;
    public string lastMessageIn;

    private string currentFen = "";
    private readonly char[] buffer = new char[1];

    public string HighlightSquares { get; set; } = "";



    void Awake()
    {
        if (instance == null)
        {
            print("NetClient Starting");
            instance = this;
            //instance.Starter();

            Thread t = new Thread(Starter);
            t.Start();

        }
        else if (instance != this)
        {
            print("NetClient already exists...Destroying Object");
            Destroy(this);
        }
    }



    public static ConcurrentQueue<string> SquareDiffQueue = new ConcurrentQueue<string>();


    void Starter()
    {
        // Method code goes here
        print("in async");

        HttpWebRequest myHttpWebRequest1 = (HttpWebRequest)WebRequest.Create($"{AddrBase}{DateTime.UtcNow.Ticks}");
        myHttpWebRequest1.KeepAlive = true;

        var response = myHttpWebRequest1.GetResponse();

        Stream streamResponse = response.GetResponseStream();
        instance.streamRead = new StreamReader(streamResponse);

        print("Connected to server");

        //StartCoroutine(ServerConnection());
        ServerConnection();
    }

    public IEnumerator ServerConnection()
    {
        while (true)
        {
            //if (instance.streamRead.Read(buffer, 0, 1) == 0)
            //{
            //    yield return new WaitForSeconds(1f);
            //}
            //else
            //{
                string lineIn = streamRead.ReadLine();

                if (!string.IsNullOrEmpty(lineIn))
                {
                    // We already have the 'd' from the read test
                    //lineIn = lineIn.Replace("ata: ", "");
                    lineIn = lineIn.Replace("data: ", "");
            
            //print(lineIn);

            JObject jObject = JObject.Parse(lineIn);


                    switch (jObject["MessageType"].Value<string>())
                    {
                        case "OnOrientationFlipped":
                            instance.lastMessageIn = lineIn;
                            //print(lineIn);
                            break;
                        case "OnRemoteFenChange":
                            //instance.lastMessageIn = lineIn;

                            string fenIn = jObject["BoardFen"].Value<string>();

                            print($"FEN in:: {fenIn}");

                            if (!string.IsNullOrEmpty(currentFen))
                            {
                                if (currentFen != fenIn)
                                {
                                    StringBuilder highlights = new StringBuilder();

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
                                    print($"DIFF::{HighlightSquares}");


                                NetClient.SquareDiffQueue.Enqueue(HighlightSquares);
                            }
                            }

                            currentFen = fenIn;
                            break;
                    }
                //}

                //yield return new WaitForSeconds(.01f);
            }
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
}
