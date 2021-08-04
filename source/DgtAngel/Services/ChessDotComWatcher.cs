using DgtAngelLib;
using System;
using System.Threading.Tasks;

namespace DgtAngel.Services
{
    public class ChessDotComWatcherEventArgs : EventArgs
    {
        public string Message { get; set; }
        public string FenString { get; set; }
    }

    public interface IChessDotComWatcher
    {
        event EventHandler<ChessDotComWatcherEventArgs> OnFenRecieved;
        event EventHandler<ChessDotComWatcherEventArgs> OnWatchStarted;
        event EventHandler<ChessDotComWatcherEventArgs> OnWatchStopped;

        Task PollChessDotComBoard();
    }

    public class ChessDotComWatcher : IChessDotComWatcher
    {
        private readonly IScriptWrapper scriptWrapper;
        private readonly IAppData appData;
        private readonly IChessDotComHelpers chessDotComHelpers;

        public event EventHandler<ChessDotComWatcherEventArgs> OnWatchStarted;
        public event EventHandler<ChessDotComWatcherEventArgs> OnWatchStopped;
        public event EventHandler<ChessDotComWatcherEventArgs> OnFenRecieved;

        public ChessDotComWatcher(IScriptWrapper scriptWrapper, IAppData appData, IChessDotComHelpers chessDotComHelpers)
        {
            this.scriptWrapper = scriptWrapper;
            this.appData = appData;
            this.chessDotComHelpers = chessDotComHelpers;
        }


        public async Task PollChessDotComBoard()
        {
            bool isWatchtNotified = false;

            for (; ; )
            {
                await scriptWrapper.WriteToConsole(ScriptWrapper.LogLevel.DEBUG, "Background", $"Loop number {appData.Age}");

                var chessDotComBoardString = await scriptWrapper.GetChessDotComBoardString();

                //0:05.8|0:07.8|bk35,wb56,wk77
                //if (chessDotComBoardString != "-")
                //{
                    try
                    {
                        await scriptWrapper.WriteToConsole(ScriptWrapper.LogLevel.DEBUG, "Background", $"Returned board is {chessDotComBoardString}");
                        string fen = chessDotComHelpers.ConvertHtmlToFenT2(chessDotComBoardString);
                        
                        if (!isWatchtNotified)
                        {
                            OnWatchStarted?.Invoke(this, new ChessDotComWatcherEventArgs() { Message = "Watch Started", FenString = "" });
                            isWatchtNotified = true;
                        }

                        OnFenRecieved?.Invoke(this, new ChessDotComWatcherEventArgs() { Message = "FEN Recieved", FenString = fen });
                    }
                    catch (Exception ex)
                    {
                        await scriptWrapper.WriteToConsole(ScriptWrapper.LogLevel.ERR, "Background", $"ChessDotCom Fen is unavailable [{ex.Message}]");

                        if (isWatchtNotified)
                        {
                            OnWatchStopped?.Invoke(this, new ChessDotComWatcherEventArgs() { Message = "Watch Stopped", FenString = "" });
                            isWatchtNotified = false;
                        }
                    }
                //}

                await Task.Delay(2000);

                appData.Age += 1;
            }
        }
    }
}
