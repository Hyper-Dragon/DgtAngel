using DgtAngelShared.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DgtAngel.Services
{
    public class ChessDotComWatcherGameStateEventArgs : EventArgs
    {
        public BoardState RemoteBoardState { get; set; }
    }

    public interface IChessDotComWatcher
    {
        event EventHandler<ChessDotComWatcherGameStateEventArgs> OnWatchStarted;
        event EventHandler<ChessDotComWatcherGameStateEventArgs> OnFenRecieved;
        event EventHandler<ChessDotComWatcherGameStateEventArgs> OnDuplicateFenRecieved;
        event EventHandler OnWatchStopped;

        Task PollChessDotComBoard(CancellationToken token);
    }

    public class ChessDotComWatcher : IChessDotComWatcher
    {
        private readonly ILogger _logger;
        private readonly IScriptWrapper _scriptWrapper;
        private readonly IChessDotComHelperService _chessDotComHelpers;

        public event EventHandler<ChessDotComWatcherGameStateEventArgs>OnWatchStarted;
        public event EventHandler<ChessDotComWatcherGameStateEventArgs> OnFenRecieved;
        public event EventHandler<ChessDotComWatcherGameStateEventArgs> OnDuplicateFenRecieved;
        public event EventHandler OnWatchStopped;

        private const int SLEEP_RUNNING_DELAY = 100;
        private const int SLEEP_REOPEN_TAB_DELAY = 3000;
        private const int SLEEP_EXCEPTION_DELAY = 10000;

        public ChessDotComWatcher(ILogger<ChessDotComWatcher> logger,
                                  IScriptWrapper scriptWrapper,
                                  IChessDotComHelperService chessDotComHelpers)
        {
            _logger = logger;
            _scriptWrapper = scriptWrapper;
            _chessDotComHelpers = chessDotComHelpers;
        }

        public async Task PollChessDotComBoard(CancellationToken token)
        {
            bool hasOnWatchStartedEventBeenFired = false;
            string lastChessDotComFenString = "";
            TurnCode lastToPlay = TurnCode.UNKNOWN;

            _logger?.LogInformation($"Started watching for tab activation");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    BoardState remoteBoardState = JsonSerializer.Deserialize<BoardState>(await _scriptWrapper.GetChessDotComBoardJson());

                    while (remoteBoardState != null && remoteBoardState.State.Code !=  ResponseCode.UNKNOWN_PAGE)
                    {
                        // If this is the first time we have found a valid page - let the outside world know that we are watching
                        if (!hasOnWatchStartedEventBeenFired)
                        {
                            _logger?.LogInformation($"Started watching chess.com");
                            OnWatchStarted?.Invoke(this, new ChessDotComWatcherGameStateEventArgs() { RemoteBoardState = remoteBoardState });
                            hasOnWatchStartedEventBeenFired = true;
                            lastChessDotComFenString = ""; //Set to blank because this is considered a new position;
                            lastToPlay = TurnCode.UNKNOWN;
                        }

                        if (lastChessDotComFenString == remoteBoardState.Board.FenString && lastToPlay == remoteBoardState.Board.Turn)
                        {
                            //_logger?.LogDebug($"This FEN and the Last FEN Match so raise the duplicateFen event.");
                            //_logger?.LogDebug($"The clock is string is [{whiteClock}] [{blackClock}] [{toPlayString}]");

                            OnDuplicateFenRecieved?.Invoke(this, new ChessDotComWatcherGameStateEventArgs() { RemoteBoardState = remoteBoardState });
                        }
                        else
                        {
                            //_logger?.LogInformation($"The FEN has changed from [{lastChessDotComFenString}] to [{newFenString}]");
                            //_logger?.LogInformation($"The clock is string is [{whiteClock}] [{blackClock}] [{toPlayString}]");

                            OnFenRecieved?.Invoke(this, new ChessDotComWatcherGameStateEventArgs() { RemoteBoardState = remoteBoardState });
                        }

                        lastChessDotComFenString = remoteBoardState.Board.FenString;
                        lastToPlay = remoteBoardState.Board.Turn;

                        _logger?.LogTrace($"Sleeping with Running Delay of {SLEEP_RUNNING_DELAY}ms");
                        await Task.Delay(SLEEP_RUNNING_DELAY, token);

                        //remoteBoardState = JsonSerializer.Deserialize<BoardState>(await _scriptWrapper.GetChessDotComBoardJson());
                        //_logger?.LogDebug($"(Inner Loop) The returned board string from _scriptWrapper.GetChessDotComBoardString() is '{chessDotComBoardString}'");

                        remoteBoardState = JsonSerializer.Deserialize<BoardState>(await _scriptWrapper.GetChessDotComBoardJson());
                    }

                    _logger?.LogTrace($"Sleeping with Reopen Tab Delay of {SLEEP_REOPEN_TAB_DELAY}ms");
                    await Task.Delay(SLEEP_REOPEN_TAB_DELAY, token);
                }
                catch (OperationCanceledException)
                {
                    //Handle termination by cancelation token
                    _logger?.LogInformation($"Terminate watch requested.");
                }
                catch (Exception ex)
                {
                    _logger?.LogInformation($"Watching Tab Stopped (Exception) - {ex.Message}");
                    _logger?.LogTrace($"Sleeping with Exception Tab Delay of {SLEEP_EXCEPTION_DELAY}ms");
                    await Task.Delay(SLEEP_EXCEPTION_DELAY, token);
                }
                finally
                {
                    if (hasOnWatchStartedEventBeenFired)
                    {
                        OnWatchStopped?.Invoke(this, new ChessDotComWatcherGameStateEventArgs());
                        _logger?.LogInformation($"The user navigated off the Chess.com game tab");
                        lastChessDotComFenString = "";
                        lastToPlay = TurnCode.UNKNOWN;
                        hasOnWatchStartedEventBeenFired = false;
                    }
                }
            }

            _logger?.LogInformation($"Stopped watching for tab activation");
        }
    }
}
