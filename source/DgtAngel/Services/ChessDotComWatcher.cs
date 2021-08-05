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
            string lastFen = "";

            for (; ; )
            {
                try
                {
                    var chessDotComBoardString = await scriptWrapper.GetChessDotComBoardString();

                    if (chessDotComBoardString != "UNDEFINED")
                    {
                        //await scriptWrapper.WriteToConsole(ScriptWrapper.LogLevel.DEBUG, "CDC_WATCH", $"Returned board is {chessDotComBoardString}");
                        string fen = chessDotComHelpers.ConvertHtmlToFenT2(chessDotComBoardString);

                        if (!isWatchtNotified)
                        {
                            OnWatchStarted?.Invoke(this, new ChessDotComWatcherEventArgs() { Message = "Watch Started", FenString = "" });
                            lastFen = "";
                            isWatchtNotified = true;
                        }

                        if (lastFen != fen)
                        {
                            OnFenRecieved?.Invoke(this, new ChessDotComWatcherEventArgs() { Message = "New FEN Recieved", FenString = fen });
                            lastFen = fen;
                        }

                        await Task.Delay(200);
                    }
                    else
                    {
                        if (isWatchtNotified)
                        {
                            OnWatchStopped?.Invoke(this, new ChessDotComWatcherEventArgs() { Message = "Watch Stopped", FenString = "" });
                            isWatchtNotified = false;
                        }
                        await Task.Delay(2000);
                    }
                }
                catch (Exception)
                {
                    lastFen = "";

                    if (isWatchtNotified)
                    {
                        OnWatchStopped?.Invoke(this, new ChessDotComWatcherEventArgs() { Message = "Watch Stopped", FenString = "" });
                        isWatchtNotified = false;
                    }
                        await Task.Delay(5000);
                    
                }

                appData.Age += 1;
            }
        }
    }
}
