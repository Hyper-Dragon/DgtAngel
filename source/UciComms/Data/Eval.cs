using System.Collections.Concurrent;
using System.Threading;

namespace UciComms.Data
{
    public record UciEngineEval(int Eval, int MateIn, string BestMove, int Depth, List<string> TopLines);

    internal sealed class Eval
    {
        private const int TOP_LINES_COUNT = 5;
        private const int BEST_MOVE_LENGTH = 4;
        private const string SCORE_PREFIX = "score cp";
        private const string MATE_PREFIX = "score mate";
        private const string PV_PREFIX = " pv ";

        private readonly ConcurrentQueue<string> lineQueue = new();
        private readonly List<Tuple<int, int, string>> topLines = new();
        private string bestMove;
        private int boardEval;

        public event Action<UciEngineEval> OnBoardEvalChanged;

        internal Eval()
        {
            Task.Factory.StartNew(ProcessLines, TaskCreationOptions.LongRunning);
        }

        internal void AddLine(string line)
        {
            lineQueue.Enqueue(line);
        }

        private void ProcessLines()
        {
            while (true)
            {
                if (lineQueue.TryDequeue(out var line))
                {
                    var lineVal = GetBoardEvalFromLine(line);
                    
                    var score = lineVal.cp;
                    var mateIn = lineVal.mateIn;
                    var depth = GetDepthFromLine(line);

                    if (score != null && depth != null)
                    {
                        

                        var newTopLines = new List<Tuple<int, int, string>>(topLines)
                        {
                            Tuple.Create(score.Value, depth.Value, line)
                        };
                        newTopLines.Sort((a, b) => b.Item2.CompareTo(a.Item2)); // sort in descending order of depth
                        if (newTopLines.Count > TOP_LINES_COUNT) newTopLines = newTopLines.Take(TOP_LINES_COUNT).ToList(); // keep only top 5 lines

                        var newBestMove = GetBestMoveFromLine(line);
                        var newBoardEval = score.Value;

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
                                OnBoardEvalChanged?.Invoke(GetEval(TOP_LINES_COUNT));
                            }
                        }
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
                var t = topLines.Take(count)
                                .Select(t => t.Item3)
                                .ToList();

                return new UciEngineEval(GetBoardEvalFromLine(t.First()).cp ?? 0,
                                         GetBoardEvalFromLine(t.First()).mateIn,
                                         GetBestMoveFromLine(t.First()),
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
            }
        }

        private static (int? cp,int mateIn) GetBoardEvalFromLine(string line)
        {
            var index = line.IndexOf(SCORE_PREFIX);
            if (index >= 0)
            {
                var score = int.Parse(line[(index + SCORE_PREFIX.Length)..].Trim().Split(' ')[0]);
                return (score,0);
            }

            index = line.IndexOf(MATE_PREFIX); // Check for mate score
            if (index >= 0)
            {
                var mateInX = int.Parse(line[(index + MATE_PREFIX.Length)..].Trim().Split(' ')[0]);
                var mateScore = mateInX > 0 ? 100000 - mateInX * 10 : -100000 - mateInX * 10;
                return (mateScore,mateInX);
            }

            return (null,0);
        }

        private static int? GetDepthFromLine(string line)
        {
            var index = line.IndexOf("depth");
            if (index < 0) return null;
            var depth = int.Parse(line.Substring(index + 6).Split()[0]);
            return depth;
        }

        private static string GetBestMoveFromLine(string line)
        {
            var index = line.IndexOf(PV_PREFIX);
            if (index < 0) return null;
            var bestMove = line.Substring(index + PV_PREFIX.Length, BEST_MOVE_LENGTH);
            return bestMove;
        }
    }
}