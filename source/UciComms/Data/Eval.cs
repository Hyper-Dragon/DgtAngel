using System.Collections.Concurrent;

namespace UciComms.Data
{
    public record UciEngineEval(int Eval, int MateIn, string BestMove, int Depth, List<string> TopLines);

    internal sealed class Eval
    {
        private const int TOP_LINES_COUNT = 5;
        private const int BEST_MOVE_LENGTH = 4;
        private const string SCORE_PREFIX = "cp";
        private const string MATE_PREFIX = "mate";
        private const string PV_PREFIX = " pv";
        private const string DEPTH_PREFIX = "depth";

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

        private static (int? cp, int mateIn) GetBoardEvalFromLine(string line)
        {
            // Search for the score prefix in the UCI response string.
            int index = line.IndexOf(SCORE_PREFIX);
            
            if (index >= 0)
            {
                // Extract the score from the UCI response.
                int score = int.Parse(line[(index + SCORE_PREFIX.Length)..].Trim().Split(' ')[0]);
                
                // Return the score and an indicator that it is not a mate score.
                return (score, 0);
            }

            // If the score prefix wasn't found, search for the mate prefix.
            index = line.IndexOf(MATE_PREFIX);
            
            if (index >= 0)
            {
                // Extract the mate-in-x value from the UCI response.
                int mateInX = int.Parse(line[(index + MATE_PREFIX.Length)..].Trim().Split(' ')[0]);
                
                // Calculate the mate score from the mate-in-x value.
                // Positive values indicate a white win and negative values indicate a black win.
                int mateScore = mateInX > 0 ? 100000 - (mateInX * 10) : -100000 - (mateInX * 10);
                
                // Return the mate score and the mate-in-x value.
                return (mateScore, mateInX);
            }

            // If neither prefix was found in the UCI response, return null values.
            return (null, 0);
        }

        private static int? GetDepthFromLine(string line)
        {
            int index = line.IndexOf(DEPTH_PREFIX);
            if (index < 0)
            {
                return null;
            }

            int depth = int.Parse(line[(index + 6)..].Split()[0]);
            return depth;
        }

        private static string? GetBestMoveFromLine(string line)
        {
            int index = line.IndexOf(PV_PREFIX);
            if (index < 0)
            {
                return null;
            }

            string bestMove = line.Substring(index + PV_PREFIX.Length, BEST_MOVE_LENGTH);
            return bestMove;
        }
    }
}