using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DgtAngel.Services
{
    public class ChessDotComWatcherGameStateEventArgs : EventArgs
    {
        public string FenString { get; set; }
        public string ToMove { get; set; }
        public string WhiteClock { get; set; }
        public string BlackClock { get; set; }
        public string IsWhiteBottom { get; set; }
    }

    public interface IChessDotComWatcher
    {
        event EventHandler OnWatchStarted;
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

        public event EventHandler OnWatchStarted;
        public event EventHandler<ChessDotComWatcherGameStateEventArgs> OnFenRecieved;
        public event EventHandler<ChessDotComWatcherGameStateEventArgs> OnDuplicateFenRecieved;
        public event EventHandler OnWatchStopped;

        private const int SLEEP_RUNNING_DELAY = 100;
        private const int SLEEP_REOPEN_TAB_DELAY = 1000;
        private const int SLEEP_EXCEPTION_DELAY = 5000;

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
            string lastChessDotComFenString = "", lastToPlay = "";

            _logger?.LogInformation($"Started watching for tab activation (https://www.chess.com/live)");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    token.ThrowIfCancellationRequested();

                    string chessDotComBoardString = await _scriptWrapper.GetChessDotComBoardString();
                    _logger?.LogDebug($"The returned board string from _scriptWrapper.GetChessDotComBoardString() is '{chessDotComBoardString}'");

                    //TODO: Split this out
                    while (!string.IsNullOrWhiteSpace(chessDotComBoardString) && chessDotComBoardString != "UNDEFINED")
                    {
                        _logger?.LogDebug($"We probably have a valid board string so lets parse it...");
                        (string newFenString, string whiteClock, string blackClock, string toPlayString, string isWhiteBottom) = _chessDotComHelpers.ConvertLiveBoardHtmlToFen(chessDotComBoardString);

                        _logger?.LogDebug($"...and the result is {newFenString} {whiteClock} {blackClock} {toPlayString}");

                        whiteClock = _chessDotComHelpers.FormatClockStringToDGT(whiteClock);
                        blackClock = _chessDotComHelpers.FormatClockStringToDGT(blackClock);

                        _logger?.LogDebug($"...then the clocks are reformatted to {whiteClock} {blackClock}");

                        // If this is the first time we have found a valid page let the outside
                        // world know that we are watching
                        if (!hasOnWatchStartedEventBeenFired)
                        {
                            _logger?.LogInformation($"Started watching chess.com");
                            OnWatchStarted?.Invoke(this, new EventArgs());
                            hasOnWatchStartedEventBeenFired = true;
                            lastChessDotComFenString = ""; //Set to blank because this is considered a new position;
                            lastToPlay = "";
                        }

                        if (lastChessDotComFenString == newFenString && lastToPlay == toPlayString)
                        {
                            _logger?.LogDebug($"This FEN and the Last FEN Match so raise the duplicateFen event.");
                            _logger?.LogDebug($"The clock is string is [{whiteClock}] [{blackClock}] [{toPlayString}]");

                            OnDuplicateFenRecieved?.Invoke(this, new ChessDotComWatcherGameStateEventArgs() { FenString = newFenString, WhiteClock = whiteClock, BlackClock = blackClock, ToMove = toPlayString, IsWhiteBottom = isWhiteBottom });
                        }
                        else
                        {
                            _logger?.LogInformation($"The FEN has changed from [{lastChessDotComFenString}] to [{newFenString}]");
                            _logger?.LogInformation($"The clock is string is [{whiteClock}] [{blackClock}] [{toPlayString}]");

                            OnFenRecieved?.Invoke(this, new ChessDotComWatcherGameStateEventArgs() { FenString = newFenString, WhiteClock = whiteClock, BlackClock = blackClock, ToMove = toPlayString, IsWhiteBottom = isWhiteBottom });
                        }

                        lastChessDotComFenString = newFenString;
                        lastToPlay = toPlayString;

                        _logger?.LogTrace($"Sleeping with Running Delay of {SLEEP_RUNNING_DELAY}ms");
                        await Task.Delay(SLEEP_RUNNING_DELAY, token);

                        chessDotComBoardString = await _scriptWrapper.GetChessDotComBoardString();
                        _logger?.LogDebug($"(Inner Loop) The returned board string from _scriptWrapper.GetChessDotComBoardString() is '{chessDotComBoardString}'");
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
                    _logger?.LogWarning($"Watching Tab Stopped (Exception) - {ex.Message}");
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
                        lastToPlay = "";
                        hasOnWatchStartedEventBeenFired = false;
                    }
                }
            }

            _logger?.LogInformation($"Stopped watching for tab activation (https://www.chess.com/live)");
        }
    }
}
