using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UciComms.Data
{
    public class Eval
    {
        private const int TOP_LINES_COUNT = 5;
        private const int BEST_MOVE_LENGTH = 4;
        private const string SCORE_PREFIX = "score cp";
        private const string PV_PREFIX = " pv ";

        private readonly ConcurrentQueue<string> _lineQueue = new();
        private readonly List<Tuple<int, int, string>> _topLines = new();
        private string _bestMove;
        private int _boardEval;

        public event EventHandler<string> OnNewBestMove;
        public event EventHandler<int> OnBoardEvalChanged;

        public Eval()
        {
            Task.Factory.StartNew(ProcessLines, TaskCreationOptions.LongRunning);
        }

        public void AddLine(string line)
        {
            _lineQueue.Enqueue(line);
        }

        private void ProcessLines()
        {
            while (true)
            {
                if (_lineQueue.TryDequeue(out var line))
                {
                    var score = GetBoardEvalFromLine(line);
                    var depth = GetDepthFromLine(line);

                    if (score != null && depth != null)
                    {
                        var newTopLines = new List<Tuple<int, int, string>>(_topLines)
                        {
                            Tuple.Create(score.Value, depth.Value, line)
                        };
                        newTopLines.Sort((a, b) => b.Item2.CompareTo(a.Item2)); // sort in descending order of depth
                        if (newTopLines.Count > TOP_LINES_COUNT) newTopLines = newTopLines.Take(TOP_LINES_COUNT).ToList(); // keep only top 5 lines

                        var newBestMove = GetBestMoveFromLine(line);
                        var newBoardEval = score.Value;

                        lock (_topLines)
                        {
                            _topLines.Clear();
                            _topLines.AddRange(newTopLines);

                            if (newBestMove != null && (_bestMove == null || newBestMove != _bestMove))
                            {
                                _bestMove = newBestMove;
                                OnNewBestMove?.Invoke(this, _bestMove);
                            }

                            if (newBoardEval != _boardEval)
                            {
                                _boardEval = newBoardEval;
                                OnBoardEvalChanged?.Invoke(this, _boardEval);
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

        public int GetBoardEval()
        {
            lock (_topLines)
            {
                return _boardEval;
            }
        }

        public List<string> GetTopLines(int count)
        {
            lock (_topLines)
            {
                return _topLines.Take(count).Select(t => t.Item3).ToList();
            }
        }

        public string GetBestMove()
        {
            lock (_topLines)
            {
                return _bestMove;
            }
        }

        private static int? GetBoardEvalFromLine(string line)
        {
            var index = line.IndexOf(SCORE_PREFIX);
            if (index < 0) return null;
            var score = int.Parse(line.Substring(index + SCORE_PREFIX.Length).Trim().Split(' ')[0]);
            return score;
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

