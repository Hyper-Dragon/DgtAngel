using DgtAngelShared.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DgtAngel.Services
{
    public class RemoteBoardWatcherGameStateEventArgs : EventArgs
    {
        public BoardState RemoteBoardState { get; set; }
    }

    public interface IRemoteBoardWatcher
    {
        event EventHandler<RemoteBoardWatcherGameStateEventArgs> OnWatchStarted;
        event EventHandler<RemoteBoardWatcherGameStateEventArgs> OnBoardStateRecieved;
        event EventHandler OnWatchStopped;

        Task PollChessDotComBoard(CancellationToken token);
    }

    public sealed class ChessDotComWatcher : IRemoteBoardWatcher
    {
        private readonly ILogger _logger;
        private readonly IScriptWrapper _scriptWrapper;

        public event EventHandler<RemoteBoardWatcherGameStateEventArgs> OnWatchStarted;
        public event EventHandler<RemoteBoardWatcherGameStateEventArgs> OnBoardStateRecieved;
        public event EventHandler OnWatchStopped;

        private const int SLEEP_RUNNING_DELAY = 100;
        private const int SLEEP_REOPEN_TAB_DELAY = 3000;
        private const int SLEEP_EXCEPTION_DELAY = 10000;

        public ChessDotComWatcher(ILogger<ChessDotComWatcher> logger,
                                  IScriptWrapper scriptWrapper)
        {
            _logger = logger;
            _scriptWrapper = scriptWrapper;
        }

        public async Task PollChessDotComBoard(CancellationToken token)
        {
            bool hasOnWatchStartedEventBeenFired = false;
            TurnCode lastToPlay = TurnCode.UNKNOWN;

            _logger?.LogInformation($"Started watching for tab activation");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    BoardState remoteBoardState = JsonSerializer.Deserialize<BoardState>(await _scriptWrapper.GetChessDotComBoardJson());

                    while (remoteBoardState != null && remoteBoardState.State.Code != ResponseCode.UNKNOWN_PAGE)
                    {
                        // If this is the first time we have found a valid page - let the outside world know that we are watching
                        if (!hasOnWatchStartedEventBeenFired)
                        {
                            _logger?.LogInformation($"Started watching chess.com");
                            OnWatchStarted?.Invoke(this, new RemoteBoardWatcherGameStateEventArgs() { RemoteBoardState = remoteBoardState });
                            hasOnWatchStartedEventBeenFired = true;
                            lastToPlay = TurnCode.UNKNOWN;
                        }

                        OnBoardStateRecieved?.Invoke(this, new RemoteBoardWatcherGameStateEventArgs() { RemoteBoardState = remoteBoardState });

                        lastToPlay = remoteBoardState.Board.Turn;

                        _logger?.LogTrace($"Sleeping with Running Delay of {SLEEP_RUNNING_DELAY}ms");
                        await Task.Delay(SLEEP_RUNNING_DELAY, token);

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
                        OnWatchStopped?.Invoke(this, new RemoteBoardWatcherGameStateEventArgs());
                        _logger?.LogInformation($"The user navigated off the Chess.com game tab");
                        lastToPlay = TurnCode.UNKNOWN;
                        hasOnWatchStartedEventBeenFired = false;
                    }
                }
            }

            _logger?.LogInformation($"Stopped watching for tab activation");
        }
    }
}
