using System.Diagnostics;
using System.Text.RegularExpressions;
using UciComms.Data;

namespace UciComms
{
    public sealed partial class UciChessEngine
    {
        // Subscribe to this events to get the parsed output from the engine
        public event EventHandler<UciResponse> OnOutputRecieved;

        // Subscribe to these events to get the raw output from the engine
        // This is useful for debugging
        public event EventHandler<string> OnOutputRecievedRaw;
        public event EventHandler<string> OnErrorRecievedRaw;
        public event EventHandler<string> OnInputSentRaw;

        public event Action<UciEngineEval> OnBoardEvalChanged;

        private static readonly Regex OptionRegex = LineIn();

        private Process? RunningProcess { get; set; }
        public FileInfo Executable { get; init; }
        public bool IsRunning => RunningProcess != null;
        public bool IsUciOk { get; private set; } = false;
        public bool IsReady { get; private set; } = false;

        private Eval evaluation = new();


        public string EngineName { get; private set; } = "UNKNOWN";
        public string EngineAuthor { get; private set; } = "UNKNOWN";

        public Dictionary<string, UciOption> Options { get; } = new();


        internal UciChessEngine(FileInfo executable)
        {
            Executable = executable;
            RunningProcess = null;

            evaluation.OnBoardEvalChanged += Evaluation_OnBoardEvalChanged; 
        }

        private void Evaluation_OnBoardEvalChanged(UciEngineEval obj)
        {
            OnBoardEvalChanged?.Invoke(obj);
        }

        [GeneratedRegex("option name (?<name>\\S+) type (?<type>\\S+)(?: default (?<default>\\S+))?(?: min (?<min>\\S+))?(?: max (?<max>\\S+))?(?: var (?<var>\\S+))?")]
        private static partial Regex LineIn();

        internal void StartUciEngine()
        {
            RunningProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Executable.FullName,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true
                }
            };

            RunningProcess.OutputDataReceived += (sender, args) =>
            {
                if (args == null) return;

                OnOutputRecievedRaw?.Invoke(this, args.Data);

                if (args.Data == "uciok")
                {
                    IsUciOk = true;
                }

                if (args.Data == "readyok")
                {
                    IsReady = true;
                }

                UciResponse? response = ParseResponse(args.Data);
                if (response != null)
                {
                    if (response is IdResponse)
                    {
                        EngineName = string.IsNullOrEmpty(((IdResponse)response).Name) ? EngineName : ((IdResponse)response).Name;
                        EngineAuthor = string.IsNullOrEmpty(((IdResponse)response).Author) ? EngineAuthor : ((IdResponse)response).Author;
                    }

                    OnOutputRecieved?.Invoke(this, response);
                }

            };

            RunningProcess.ErrorDataReceived += (sender, args) => OnErrorRecievedRaw?.Invoke(this, args?.Data ?? "");

            try
            {
                _ = RunningProcess.Start();
            }
            catch (Exception) 
            {
                RunningProcess = null;
                throw;
            }

            RunningProcess.BeginErrorReadLine();
            RunningProcess.BeginOutputReadLine();

            SendCommand("uci");

            if (!WaitForUciOk())
            {
                //If message not recieved then the exe isn't a UCI engine so kill the process
                RunningProcess.Kill();
                throw new Exception("Executable is not a UCI engine.");
            }

            SendCommand("isready");
            
            if(!WaitForReady())
            {
                RunningProcess.Kill();
                throw new Exception("UCI engine ready timeout expired.");
            }
        }

        private void SendCommand(string command)
        {
            OnInputSentRaw?.Invoke(this, command);
            RunningProcess?.StandardInput?.WriteLine(command);
            RunningProcess?.StandardInput?.Flush();
        }

        internal void StopUciEngine()
        {
            if (IsRunning)
            {
                RunningProcess?.Kill();
                RunningProcess?.Dispose();
                RunningProcess = null;
            }
        }

        public void SetOption(string name, string value)
        {
            if (Options.ContainsKey(name))
            {
                Options[name].VarValue = value;
                SendCommand($"setoption name {name} value {value}");
            }
        }

        public void SetPosition(string fen)
        {
            SendCommand("ucinewgame");
            SendCommand($"position fen {fen}");
        }

        public void SetDebug(bool debugOn)
        {
            SendCommand($"debug {(debugOn ? "on" : "off")}");
        }


        public void SetMoves(string moves)
        {
            SendCommand($"position startpos moves {moves}");
        }

        public void Go(int depth)
        {
            SendCommand($"go depth {depth}");
        }

        public void GoInfinite()
        {
            SendCommand($"go infinite");
        }

        public bool WaitForUciOk()
        {
            int loop = 0;
            while (IsUciOk == false && loop < 30)
            {
                Thread.Sleep(100);
                loop++;
            }

            return IsUciOk;
        }

        public bool WaitForReady()
        {
            int loop = 0;
            while (IsReady == false && loop < 30)
            {
                Thread.Sleep(100);
                loop++;
            }

            return IsReady;
        }

        public void Stop()
        {
            SendCommand("stop");
        }

        public void Quit()
        {
            SendCommand("quit");
            RunningProcess?.WaitForExit();
        }


        public UciResponse? ParseResponse(string rawData)
        {
            if (string.IsNullOrWhiteSpace(rawData))
            {
                return null;
            }

            if (rawData.StartsWith("id"))
            {
                string[] parts = rawData.Split(' ');
                IdResponse idResponse = new() { RawData = rawData };

                for (int i = 0; i < parts.Length - 1; i++)
                {
                    if (parts[i] == "name")
                    {
                        idResponse.Name = string.Join(" ", parts.Skip(i + 1)).Trim();
                    }
                    else if (parts[i] == "author")
                    {
                        idResponse.Author = string.Join(" ", parts.Skip(i + 1)).Trim();
                    }
                }

                return idResponse;
            }
            else if (rawData == "uciok")
            {
                return new UciOkResponse { RawData = rawData };
            }
            else if (rawData == "readyok")
            {
                return new ReadyOkResponse { RawData = rawData };
            }
            else if (rawData.StartsWith("option"))
            {
                var match = OptionRegex.Match(rawData);
                if (match.Success)
                {
                    var name = match.Groups["name"].Value;
                    var type = match.Groups["type"].Value;
                    var defaultVal = match.Groups["default"].Success ? match.Groups["default"].Value : null;
                    var minVal = match.Groups["min"].Success ? match.Groups["min"].Value : null;
                    var maxVal = match.Groups["max"].Success ? match.Groups["max"].Value : null;
                    var varVal = match.Groups["var"].Success ? match.Groups["var"].Value : null;

                    var option = new UciOption(name, type, defaultVal ?? "", minVal ?? "", maxVal ?? "", varVal ?? "");

                    if (Options.ContainsKey(name))
                    {
                        Options[name] = option;
                    }
                    else
                    {
                        Options.Add(name, option);
                    }
                }
            }
            else if (rawData.StartsWith("bestmove"))
            {
                string[] splitStr = rawData.Split(' ');

                return new BestMoveResponse
                {
                    RawData = rawData,
                    BestMove = splitStr[1],
                    PonderMove = splitStr.Length == 4 ? splitStr[3] : ""
                };
            }
            else if (rawData.StartsWith("info"))
            {
                string[] splitStr = rawData.Split(' ');
                InfoResponse infoResponse = new() { RawData = rawData };

                for (int i = 1; i < splitStr.Length; i++)
                {
                    switch (splitStr[i])
                    {
                        case "depth": infoResponse.Depth = int.Parse(splitStr[++i]); break;
                        case "seldepth": infoResponse.SelDepth = int.Parse(splitStr[++i]); break;
                        case "time": infoResponse.Time = int.Parse(splitStr[++i]); break;
                        case "nodes": infoResponse.Nodes = decimal.Parse(splitStr[++i]); break;
                        case "pv": infoResponse.Pv = string.Join(" ", splitStr[++i..]); i = splitStr.Length; break;
                        case "multipv": infoResponse.MultiPv = int.Parse(splitStr[++i]); break;
                        case "score":

                            //Send to eval function
                            evaluation.AddLine(rawData);

                            i++;
                            if (splitStr[i] == "cp")
                            {
                                infoResponse.ScoreCp = int.Parse(splitStr[++i]);
                            }
                            else if (splitStr[i] == "mate")
                            {
                                infoResponse.ScoreMate = int.Parse(splitStr[++i]);
                            }

                            if (i + 1 < splitStr.Length && (splitStr[i + 1] == "lowerbound" || splitStr[i + 1] == "upperbound"))
                            {
                                infoResponse.ScoreBound = splitStr[++i];
                            }
                            break;
                        case "currmove": infoResponse.CurrMove = splitStr[++i]; break;
                        case "currmovenumber": infoResponse.CurrMoveNumber = int.Parse(splitStr[++i]); break;
                        case "hashfull": infoResponse.HashFull = int.Parse(splitStr[++i]); break;
                        case "nps": infoResponse.Nps = int.Parse(splitStr[++i]); break;
                        case "tbhits": infoResponse.TbHits = int.Parse(splitStr[++i]); break;
                        case "sbhits": infoResponse.SbHits = int.Parse(splitStr[++i]); break;
                        case "cpuload": infoResponse.CpuLoad = int.Parse(splitStr[++i]); break;
                        case "string": infoResponse.StringInfo = string.Join(" ", splitStr[++i..]); i = splitStr.Length; break;
                        case "refutation": infoResponse.Refutation = string.Join(" ", splitStr[++i..]); i = splitStr.Length; break;
                        case "currline":
                            infoResponse.CurrLineCpuNr = int.TryParse(splitStr[++i], out int cpuNr) ? cpuNr : 1;
                            if (infoResponse.CurrLineCpuNr != 1)
                            {
                                i++;
                            }

                            infoResponse.CurrLine = string.Join(" ", splitStr[i..]); i = splitStr.Length;
                            break;
                    }
                }

                return infoResponse;
            }

            return null;
        }
    }
}
