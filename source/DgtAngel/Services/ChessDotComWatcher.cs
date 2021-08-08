using DgtAngelLib;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DgtAngel.Services
{
    public class ChessDotComWatcherEventArgs : EventArgs
    {
        public string Message { get; set; }
        public string FenString { get; set; } = "";
        public string ToMove { get; set; } = "-";
        public string WhiteClock { get; set; } = "00:00";
        public string BlackClock { get; set; } = "00:00";
    }

    public interface IChessDotComWatcher
    {
        event EventHandler<ChessDotComWatcherEventArgs> OnFenRecieved;
        event EventHandler<ChessDotComWatcherEventArgs> OnWatchStarted;
        event EventHandler<ChessDotComWatcherEventArgs> OnWatchStopped;

        Task PollChessDotComBoard(CancellationToken token);
    }

    public class ChessDotComWatcher : IChessDotComWatcher
    {
        private readonly IScriptWrapper scriptWrapper;
        private readonly IChessDotComHelpers chessDotComHelpers;

        public event EventHandler<ChessDotComWatcherEventArgs> OnWatchStarted;
        public event EventHandler<ChessDotComWatcherEventArgs> OnWatchStopped;
        public event EventHandler<ChessDotComWatcherEventArgs> OnFenRecieved;

        private const int SLEEP_RUNNING_DELAY = 100;
        private const int SLEEP_REOPEN_TAB_DELAY = 1000;
        private const int SLEEP_EXCEPTION_DELAY = 5000;

        private const string MSG_SRC = "CDC_WATCH";

        public ChessDotComWatcher(IScriptWrapper scriptWrapper, IChessDotComHelpers chessDotComHelpers)
        {
            this.scriptWrapper = scriptWrapper;
            this.chessDotComHelpers = chessDotComHelpers;
        }

        public async Task PollChessDotComBoard(CancellationToken token)
        {
            await scriptWrapper.WriteToConsole(ScriptWrapper.LogLevel.INFO, MSG_SRC, $"Started watching for tab activation (https://www.chess.com/live)");

            bool hasWatchStartedBeenEventFired = false;
            string chessDotComBoardString = "";
            string lastChessDotComBoardString = "";

            for (; ; )
            {
                try
                {
                    token.ThrowIfCancellationRequested();

                    chessDotComBoardString = await scriptWrapper.GetChessDotComBoardString();
                    await scriptWrapper.WriteToConsole(ScriptWrapper.LogLevel.DEBUG, MSG_SRC, $"Returned board string is {chessDotComBoardString}");

                    if (chessDotComBoardString != "UNDEFINED")
                    {
                        await scriptWrapper.WriteToConsole(ScriptWrapper.LogLevel.DEBUG, MSG_SRC, $"Trying to calculate the FEN from the board string.");
                        (string fen, string whiteClock, string blackClock, string toPlay) = chessDotComHelpers.ConvertHtmlToFenT2(chessDotComBoardString);

                        if (!hasWatchStartedBeenEventFired)
                        {
                            await scriptWrapper.WriteToConsole(ScriptWrapper.LogLevel.DEBUG, MSG_SRC, $"This is the first FEN found so raising the OnWatchStarted Event.");
                            OnWatchStarted?.Invoke(this, new ChessDotComWatcherEventArgs() { Message = $"{MSG_SRC}:Watching Tab Started" });
                            lastChessDotComBoardString = "";
                            hasWatchStartedBeenEventFired = true;
                        }

                        if (lastChessDotComBoardString != chessDotComBoardString)
                        {
                            await scriptWrapper.WriteToConsole(ScriptWrapper.LogLevel.DEBUG, MSG_SRC, $"The FEN has changed from {lastChessDotComBoardString} to {chessDotComBoardString}");
                            OnFenRecieved?.Invoke(this, new ChessDotComWatcherEventArgs() { Message = $"{MSG_SRC}:New FEN Recieved", FenString = fen, WhiteClock=whiteClock, BlackClock=blackClock, ToMove=toPlay });
                            lastChessDotComBoardString = chessDotComBoardString;
                        }

                        await scriptWrapper.WriteToConsole(ScriptWrapper.LogLevel.DEBUG, MSG_SRC, $"Sleeping with Running Delay of {SLEEP_RUNNING_DELAY}ms");
                        await Task.Delay(SLEEP_RUNNING_DELAY,token);
                    }
                    else
                    {
                        if (hasWatchStartedBeenEventFired)
                        {
                            OnWatchStopped?.Invoke(this, new ChessDotComWatcherEventArgs() { Message = $"{MSG_SRC}:Watching Tab Stopped (No FEN)" });
                            hasWatchStartedBeenEventFired = false;
                        }

                        await scriptWrapper.WriteToConsole(ScriptWrapper.LogLevel.DEBUG, MSG_SRC, $"Sleeping with Reopen Tab Delay of {SLEEP_REOPEN_TAB_DELAY}ms");
                        await Task.Delay(SLEEP_REOPEN_TAB_DELAY,token);
                    }
                }
                catch (OperationCanceledException)
                {
                    //Handle termination by cancelation token
                    await scriptWrapper.WriteToConsole(ScriptWrapper.LogLevel.DEBUG, MSG_SRC, $"Terminate watch requested.");
                    if (hasWatchStartedBeenEventFired)
                    {
                        OnWatchStopped?.Invoke(this, new ChessDotComWatcherEventArgs() { Message = $"{MSG_SRC}:Watching Tab Stopped (Requested)" });
                        hasWatchStartedBeenEventFired = false;
                    }

                    break;
                }
                catch (Exception ex)
                {
                    await scriptWrapper.WriteToConsole(ScriptWrapper.LogLevel.DEBUG, MSG_SRC, $"Watching Tab Stopped (Exception) - {ex.Message}");
                    lastChessDotComBoardString = "";

                    if (hasWatchStartedBeenEventFired)
                    {
                        OnWatchStopped?.Invoke(this, new ChessDotComWatcherEventArgs() { Message = $"{MSG_SRC}:Watching Tab Stopped (Exception) - {ex.Message}" });
                        hasWatchStartedBeenEventFired = false;
                    }

                    await scriptWrapper.WriteToConsole(ScriptWrapper.LogLevel.DEBUG, MSG_SRC, $"Sleeping with Exception Tab Delay of {SLEEP_EXCEPTION_DELAY}ms");
                    await Task.Delay(SLEEP_EXCEPTION_DELAY, token);                   
                }
            }

            await scriptWrapper.WriteToConsole(ScriptWrapper.LogLevel.INFO, MSG_SRC, $"Stopped watching for tab activation (https://www.chess.com/live)");
        }
    }
}
