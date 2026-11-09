using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UciComms.Data;

namespace UciComms
{
    public sealed partial class UciChessEngine
    {
        // Subscribe to this events to get the parsed output from the engine
        internal event EventHandler<UciResponse>? OnOutputRecieved;

        // Subscribe to these events to get the raw output from the engine
        internal event EventHandler<string>? OnOutputRecievedRaw;
        internal event EventHandler<string>? OnErrorRecievedRaw;
        internal event EventHandler<string>? OnInputSentRaw;

        // Subscribe to this event to get the board evaluation from the engine
        internal event Action<UciEngineEval>? OnBoardEvalChanged;

        private static readonly Regex OptionRegex = LineIn();

        internal  Process? RunningProcess { get; set; }
        internal FileInfo Executable { get; init; }
        internal bool IsRunning => RunningProcess != null;
        internal bool IsUciOk { get; private set; }
        internal bool IsReady { get; private set; }
        
        internal  Eval evaluation = new();


        internal string EngineName { get; private set; } = "UNKNOWN";
        internal string EngineAuthor { get; private set; } = "UNKNOWN";
        internal Dictionary<string, UciOption> Options { get; } = new();
        internal string LastSeenFen { get; private set; } = "";
        internal string LastSeenMoveList { get; private set; } = "";


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

        [GeneratedRegex("^option name (?<name>.+?) type (?<type>\\S+)(?: default (?<default>\\S+))?(?: min (?<min>\\S+))?(?: max (?<max>\\S+))?(?: var (?<var>\\S+))?")]
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
                if (args == null || args.Data == null) { return; }


                OnOutputRecievedRaw?.Invoke(this, (args?.Data ?? ""));

                if ((args?.Data ?? "") == "uciok")
                {
                    IsUciOk = true;
                }
                else if ((args?.Data ?? "") == "readyok")
                {
                    IsReady = true;
                }

                UciResponse? response = ParseResponse(args?.Data ?? "");
                if (response != null)
                {
                    if (response is IdResponse idResponse)
                    {
                        EngineName = string.IsNullOrEmpty(idResponse.Name) ? EngineName : idResponse.Name;
                        EngineAuthor = string.IsNullOrEmpty(idResponse.Author) ? EngineAuthor : idResponse.Author;
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

            if (!WaitForReady())
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

        internal void SetOption(string name, string value)
        {
            if (Options.TryGetValue(name, out UciOption? existing) && existing is not null)
            {
                existing.VarValue = value;
                SendCommand($"setoption name {name} value {value}");
            }
        }

        internal void SetPosition(string fen)
        {
            evaluation.ResetEval();
            SendCommand("ucinewgame");
            SendCommand($"position fen {fen}");
            LastSeenFen = fen;
        }

        internal void SetDebug(bool debugOn)
        {
            SendCommand($"debug {(debugOn ? "on" : "off")}");
        }


        internal void SetMoves(string moves)
        {
            SendCommand($"position startpos moves {moves}");
            LastSeenMoveList = moves;
        }

        internal void Go(int depth)
        {
            SendCommand($"go depth {depth}");
        }

        internal void GoInfinite()
        {
            SendCommand($"go infinite");
        }

        internal bool WaitForUciOk()
        {
            int loop = 0;
            while (IsUciOk == false && loop < 30)
            {
                Thread.Sleep(100);
                loop++;
            }

            return IsUciOk;
        }

        internal bool WaitForReady()
        {
            int loop = 0;
            while (IsReady == false && loop < 30)
            {
                Thread.Sleep(100);
                loop++;
            }

            return IsReady;
        }

        internal void Stop()
        {
            SendCommand("stop");
        }

        internal void Quit()
        {
            SendCommand("quit");
            RunningProcess?.WaitForExit();
        }


        internal UciResponse? ParseResponse(string rawData)
        {
            if (string.IsNullOrWhiteSpace(rawData))
            {
                return null;
            }

            if (rawData.StartsWith("id", StringComparison.Ordinal))
            {
                string[] parts = rawData.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                IdResponse idResponse = new() { RawData = rawData };

                int nameIndex = Array.IndexOf(parts, "name");
                int authorIndex = Array.IndexOf(parts, "author");

                if (nameIndex >= 0)
                {
                    // The name ends where the author section starts, if there is one.
                    int nameEnd = authorIndex > nameIndex ? authorIndex : parts.Length;
                    idResponse.Name = string.Join(" ", parts[(nameIndex + 1)..nameEnd]).Trim();
                }

                if (authorIndex >= 0 && authorIndex + 1 < parts.Length)
                {
                    idResponse.Author = string.Join(" ", parts[(authorIndex + 1)..]).Trim();
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
            else if (rawData.StartsWith("option", StringComparison.Ordinal))
            {
                Match match = OptionRegex.Match(rawData);
                if (match.Success)
                {
                    string name = match.Groups["name"].Value;
                    string type = match.Groups["type"].Value;
                    string? defaultVal = match.Groups["default"].Success ? match.Groups["default"].Value : null;
                    string? minVal = match.Groups["min"].Success ? match.Groups["min"].Value : null;
                    string? maxVal = match.Groups["max"].Success ? match.Groups["max"].Value : null;
                    string? varVal = match.Groups["var"].Success ? match.Groups["var"].Value : null;

                    UciOption option = new(name, type, defaultVal ?? "", minVal ?? "", maxVal ?? "", varVal ?? "");

                    if (!Options.TryAdd(name, option))
                    {
                        Options[name] = option;
                    }
                }
            }
            else if (rawData.StartsWith("bestmove", StringComparison.Ordinal))
            {
                string[] splitStr = rawData.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                int ponderIndex = Array.IndexOf(splitStr, "ponder");

                return new BestMoveResponse
                {
                    RawData = rawData,
                    // Engines emit a bare "bestmove" when they have no move to give.
                    BestMove = splitStr.Length > 1 ? splitStr[1] : "",
                    PonderMove = (ponderIndex >= 0 && ponderIndex + 1 < splitStr.Length) ? splitStr[ponderIndex + 1] : ""
                };
            }
            else if (rawData.StartsWith("info", StringComparison.Ordinal))
            {
                string[] splitStr = rawData.Split(' ');
                InfoResponse infoResponse = new() { RawData = rawData };

                for (int i = 1; i < splitStr.Length; i++)
                {
                    switch (splitStr[i])
                    {
                        case "depth": infoResponse.Depth = int.Parse(splitStr[++i], CultureInfo.InvariantCulture); break;
                        case "seldepth": infoResponse.SelDepth = int.Parse(splitStr[++i], CultureInfo.InvariantCulture); break;
                        case "time": infoResponse.Time = int.Parse(splitStr[++i], CultureInfo.InvariantCulture); break;
                        case "nodes": infoResponse.Nodes = decimal.Parse(splitStr[++i], CultureInfo.InvariantCulture); break;
                        case "pv": infoResponse.Pv = string.Join(" ", splitStr[++i..]); i = splitStr.Length; break;
                        case "multipv": infoResponse.MultiPv = int.Parse(splitStr[++i], CultureInfo.InvariantCulture); break;
                        case "score":
                            //Send to eval function
                            evaluation.AddLine(rawData, LastSeenFen);
                            break;
                        case "currmove": infoResponse.CurrMove = splitStr[++i]; break;
                        case "currmovenumber": infoResponse.CurrMoveNumber = int.Parse(splitStr[++i], CultureInfo.InvariantCulture); break;
                        case "hashfull": infoResponse.HashFull = int.Parse(splitStr[++i], CultureInfo.InvariantCulture); break;
                        case "nps": infoResponse.Nps = int.Parse(splitStr[++i], CultureInfo.InvariantCulture); break;
                        case "tbhits": infoResponse.TbHits = int.Parse(splitStr[++i], CultureInfo.InvariantCulture); break;
                        case "sbhits": infoResponse.SbHits = int.Parse(splitStr[++i], CultureInfo.InvariantCulture); break;
                        case "cpuload": infoResponse.CpuLoad = int.Parse(splitStr[++i], CultureInfo.InvariantCulture); break;
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
