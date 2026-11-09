using System.Collections.Concurrent;

namespace UciComms.Data
{
    public record UciEngineEval(int Eval, int MateIn, string BestMove, int Depth, List<string> TopLines);

    internal sealed class Eval
    {
        private const int TOP_LINES_COUNT = 5;

        // Tokens, not substrings. "cpuload" contains "cp" and "seldepth" contains
        // "depth", so searching the line for those letters finds the wrong field.
        private const string SCORE_TOKEN = "score";
        private const string SCORE_CP = "cp";
        private const string SCORE_MATE = "mate";
        private const string PV_TOKEN = "pv";
        private const string DEPTH_TOKEN = "depth";

        private readonly ConcurrentQueue<(string line,string lastSeenFen)> lineQueue = new();
        private readonly List<Tuple<int, int, string>> topLines = new();
        private string? bestMove;
        private int? boardEval;

        public event Action<UciEngineEval>? OnBoardEvalChanged;

        internal Eval()
        {
            _ = Task.Factory.StartNew(ProcessLines, TaskCreationOptions.LongRunning);
        }

        internal void AddLine(string line, string lastSeenFen)
        {
            lineQueue.Enqueue((line, lastSeenFen));
        }

        private void ProcessLines()
        {
            while (true)
            {
                if (lineQueue.TryDequeue( out (string line, string lastSeenFen) newLine) )
                {
                    (int? cp, int mateIn) lineVal = GetBoardEvalFromLine(newLine.line);

                    //bool? isWhiteTurn = newLine.lastSeenFen.Split(' ')[1] == "w";
                    bool isWhiteTurn = true;
                    int? score = lineVal.cp;
                    int? mateIn = lineVal.mateIn;
                    int? depth = GetDepthFromLine(newLine.line);


                    /*
                    

                            i++;
                            if (splitStr[i] == "cp")
                            {
                                int scoreCp = int.Parse(splitStr[++i]);
                                infoResponse.ScoreCp = isWhiteTurn ? scoreCp : -scoreCp;
                                infoResponse.ScoreMate = 0;
                            }
                            else if (splitStr[i] == "mate")
                            {
                                int scoreMate = int.Parse(splitStr[++i]);

                                infoResponse.ScoreCp = isWhiteTurn ? int.MaxValue : int.MinValue;
                                infoResponse.ScoreMate = isWhiteTurn ? scoreMate : -scoreMate;
                            }

                            if (i + 1 < splitStr.Length && (splitStr[i + 1] == "lowerbound" || splitStr[i + 1] == "upperbound"))
                            {
                                infoResponse.ScoreBound = splitStr[++i];
                            }

                    */

                    if (score != null && depth != null)
                    {
                        List<Tuple<int, int, string>> newTopLines = new(topLines)
                        {
                            Tuple.Create(score.Value, depth.Value, newLine.line)
                        };

                        newTopLines.Sort((a, b) => b.Item2.CompareTo(a.Item2)); // sort in descending order of depth
                        
                        if (newTopLines.Count > TOP_LINES_COUNT)
                        {
                            newTopLines = newTopLines.Take(TOP_LINES_COUNT).ToList(); // keep only top 5 lines
                        }

                        string newBestMove = GetBestMoveFromLine(newLine.line) ?? "";
                         
                        int newBoardEval = score.Value;

                        lock (topLines)
                        {
                            topLines.Clear();
                            topLines.AddRange(newTopLines);

                            if (newBestMove != null && (bestMove == null || newBestMove != bestMove))
                            {
                                bestMove = newBestMove;
                            }

                            if (newBoardEval != boardEval)
                            {
                                boardEval = newBoardEval;
                                //OnBoardEvalChanged?.Invoke(GetEval(TOP_LINES_COUNT));
                            }
                        }

                        OnBoardEvalChanged?.Invoke(GetEval(TOP_LINES_COUNT));
                    }
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        internal UciEngineEval GetEval(int count)
        {
            lock (topLines)
            {
                List<string> t = topLines.Take(count)
                                .Select(t => t.Item3)
                                .ToList();

                return new UciEngineEval(GetBoardEvalFromLine(t.First()).cp ?? 0,
                                         GetBoardEvalFromLine(t.First()).mateIn,
                                         GetBestMoveFromLine(t.First()) ?? "",
                                         GetDepthFromLine(t.First()) ?? 0,
                                         t);
            }
        }

        internal void ResetEval()
        {
            lock (topLines)
            {
                topLines.Clear();
                bestMove = "";
                boardEval = 0;

                OnBoardEvalChanged?.Invoke(new UciEngineEval(0,0,"",0,new List<string>()));
            }
        }

        internal static (int? cp, int mateIn) GetBoardEvalFromLine(string line)
        {
            string[] tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // The score is always "score cp <n>" or "score mate <n>".
            int scoreIndex = Array.IndexOf(tokens, SCORE_TOKEN);
            if (scoreIndex < 0 || scoreIndex + 2 >= tokens.Length)
            {
                return (null, 0);
            }

            if (!int.TryParse(tokens[scoreIndex + 2], out int value))
            {
                return (null, 0);
            }

            return tokens[scoreIndex + 1] switch
            {
                SCORE_CP => (value, 0),
                // Positive values indicate a white win, negative a black win.
                SCORE_MATE => (value > 0 ? 100000 - (value * 10) : -100000 - (value * 10), value),
                _ => ((int?)null, 0),
            };
        }

        internal static int? GetDepthFromLine(string line)
        {
            string[] tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            int index = Array.IndexOf(tokens, DEPTH_TOKEN);
            if (index < 0 || index + 1 >= tokens.Length)
            {
                return null;
            }

            return int.TryParse(tokens[index + 1], out int depth) ? depth : null;
        }

        internal static string? GetBestMoveFromLine(string line)
        {
            string[] tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            int index = Array.IndexOf(tokens, PV_TOKEN);
            if (index < 0 || index + 1 >= tokens.Length)
            {
                return null;
            }

            // The first move after "pv" is the best move, whatever its length.
            return tokens[index + 1];
        }
    }
}