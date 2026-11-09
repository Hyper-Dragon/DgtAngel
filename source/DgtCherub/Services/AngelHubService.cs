using ChessHelpers;
using DgtAngelShared.Json;
using DgtRabbitWrapper.DgtEbDll;
using DynamicBoard.Helpers;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Threading.Channels;
using UciComms;
using UciComms.Data;
using static DgtAngelShared.Json.CherubApiMessage;

namespace DgtCherub.Services
{
    public sealed class AngelHubService : IAngelHubService
    {
        public event Action OnInitComplete;
        public event Action<string> OnLocalFenChange;
        public event Action<string, string, string, string, string, string, bool> OnRemoteFenChange;
        public event Action<string, bool> OnRemoteBoardStatusChange;
        public event Action OnRemoteDisconnect;
        public event Action OnClockChange;
        public event Action OnOrientationFlipped;
        public event Action<long, int, string, string> OnBoardMissmatch;
        public event Action OnBoardMatcherStarted;
        public event Action<long> OnBoardMatchFromMissmatch;
        public event Action<string> OnRemoteWatchStarted;
        public event Action<string> OnRemoteWatchStopped;
        public event Action<string, bool> OnNewMoveDetected;
        public event Action<string> OnPlayWhiteClockAudio;
        public event Action<string> OnPlayBlackClockAudio;
        public event Action<string, string> OnNotification;
        public event Action OnPluginDisconnect;


        public bool IsClientInitComplete { get; private set; }
        public bool IsWhiteOnBottom { get; private set; } = true;
        public bool IsMismatchDetected { get; private set; }
        public bool EchoExternalMessagesToConsole { get; private set; } = true;
        public string LocalBoardFEN { get; private set; }
        public string LastMatchedPosition { get; private set; }
        public string RemoteBoardFEN { get; private set; }
        public string RemoteBoardStatusMessage { get; private set; } = "";
        public string FromRemoteBoardFEN { get; private set; }
        public string LastMove { get; private set; }
        public int WhiteClockMsRemaining { get; private set; }
        public int BlackClockMsRemaining { get; private set; }
        public string WhiteClock { get; private set; } = "00:00";
        public string BlackClock { get; private set; } = "00:00";
        public string RunWhoString { get; private set; } = "0";
        public int MatcherRemoteTimeDelayMs { get; set; } = MATCHER_REMOTE_TIME_DELAY_MS;
        public int MatcherLocalDelayMs { get; set; } = MATCHER_LOCAL_TIME_DELAY_MS;
        public int FromMismatchDelayMs { get; set; } = MATCHER_LOCAL_TIME_DELAY_FROM_MISMATCH_MS;
        public bool IsLocalBoardAvailable => !string.IsNullOrWhiteSpace(LocalBoardFEN);
        public bool IsRemoteBoardAvailable => !string.IsNullOrWhiteSpace(RemoteBoardFEN);

        /// <summary>
        /// The position last sent to the engine for analysis, or null when there has been none.
        /// </summary>
        public KibitzerPosition CurrentKibitzerPosition { get; private set; }

        /// <summary>
        /// Raised with the inferred position each time the engine is given a new one.
        /// </summary>
        public event Action<KibitzerPosition> OnKibitzerPositionChange;
        public bool IsBoardInSync { get; private set; } = true;
        public bool IsRemoteBoardStateActive => (RemoteBoardFEN != "" && WhiteClock != "00:00") || BlackClock != "00:00";
        private static Guid CurrentUpdatetMatch { get; set; } = Guid.NewGuid();


        private const int MS_IN_HOUR = 3600000;
        private const int MS_IN_MIN = 60000;
        private const int MS_IN_SEC = 1000;

        private const int MATCHER_REMOTE_TIME_DELAY_MS = 5000;
        private const int MATCHER_LOCAL_TIME_DELAY_MS = 2000;
        private const int MATCHER_LOCAL_TIME_DELAY_FROM_MISMATCH_MS = 1000;

        private const int POST_EVENT_DELAY_LAST_MOVE = MS_IN_SEC * 1;
        private const int POST_EVENT_DELAY_LOCAL_FEN = MS_IN_SEC / 2;
        private const int POST_EVENT_DELAY_REMOTE_FEN = MS_IN_SEC / 10;
        private const int POST_EVENT_DELAY_CLOCK = MS_IN_SEC / 2;
        private const int POST_EVENT_DELAY_MESSAGE = MS_IN_SEC / 10;
        private const int POST_EVENT_DELAY_ORIENTATION = MS_IN_SEC / 10;

        private readonly ILogger _logger;
        private readonly IUciEngineManager _uciEngineManager;

        /// <summary>
        /// Source of "now" and of delays. Production leaves this at
        /// <see cref="TimeProvider.System"/>; tests substitute a provider so clock
        /// arithmetic and the post-event delays are deterministic instead of real waits.
        /// </summary>
        internal TimeProvider TimeSource { get; set; } = TimeProvider.System;

        /// <summary>Queue items that are either still waiting or currently being processed.</summary>
        private int inFlight;

        /// <summary>
        /// Queues an item and counts it as outstanding until its processor finishes.
        /// Counting on enqueue rather than on read is what makes <see cref="DrainAsync"/>
        /// race-free: an item is never simultaneously not-queued and not-in-flight.
        /// </summary>
        private void Enqueue<T>(ChannelWriter<T> writer, T item)
        {
            _ = System.Threading.Interlocked.Increment(ref inFlight);

            if (!writer.TryWrite(item))
            {
                _ = System.Threading.Interlocked.Decrement(ref inFlight);
            }
        }

        /// <summary>
        /// Test hook: completes once every processor has finished the item it was holding
        /// and all queues are empty. Production never calls this. It exists so a test can
        /// assert on the outcome of a queue instead of sleeping for a guessed period.
        /// </summary>
        internal async Task DrainAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (Volatile.Read(ref inFlight) == 0 &&
                    localFenProcessChannel.Reader.Count == 0 &&
                    remoteFenProcessChannel.Reader.Count == 0 &&
                    clockProcessChannel.Reader.Count == 0 &&
                    lastMoveProcessChannel.Reader.Count == 0 &&
                    orientationProcessChannel.Reader.Count == 0 &&
                    messageProcessChannel.Reader.Count == 0)
                {
                    return;
                }

                await Task.Yield();
            }
        }

        /// <summary>Items taken by a processor but not yet finished. Test hook.</summary>
        internal int InFlightCount => Volatile.Read(ref inFlight);

        private readonly SemaphoreSlim startStopSemaphore = new(1, 1);

        private readonly Channel<string> localFenProcessChannel;
        private readonly Channel<BoardState> remoteFenProcessChannel;
        private readonly Channel<BoardState> clockProcessChannel;
        private readonly Channel<BoardState> lastMoveProcessChannel;
        private readonly Channel<bool> orientationProcessChannel;
        private readonly Channel<(string source, string message)> messageProcessChannel;

        //private readonly object matcherLockObj = new();

        private double whiteNextClockAudioNotBefore = double.MaxValue;
        private double blackNextClockAudioNotBefore = double.MaxValue;

        private string lastMoveVoiceTest = "";
        //private bool isKibitzerRunning = false;


        //public AngelHubService(ILogger<AngelHubService> logger, IDgtEbDllFacade dgtEbDllFacade)
        public AngelHubService(ILogger<AngelHubService> logger, IUciEngineManager uciEngineManager)
        {
            _logger = logger;
            _uciEngineManager = uciEngineManager;


            //_dgtEbDllFacade.OnStableFenChanged += ((obj, args) =>
            //{
            //    _uciEngineManager.LoadedEngineSetPosition(args.FEN);
            //    
            //    _uciEngineManager.LoadedEngineGoInfinite();
            //});


            BoundedChannelOptions processChannelOptions = new(3)
            {
                AllowSynchronousContinuations = true,
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = true
            };

            BoundedChannelOptions messageChannelOptions = new(100)
            {
                AllowSynchronousContinuations = true,
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = true
            };

            // Init the channels and run the processors
            localFenProcessChannel = Channel.CreateBounded<string>(processChannelOptions);
            remoteFenProcessChannel = Channel.CreateBounded<BoardState>(processChannelOptions);
            clockProcessChannel = Channel.CreateBounded<BoardState>(processChannelOptions);
            lastMoveProcessChannel = Channel.CreateBounded<BoardState>(processChannelOptions);
            orientationProcessChannel = Channel.CreateBounded<bool>(processChannelOptions);
            messageProcessChannel = Channel.CreateBounded<(string source, string message)>(messageChannelOptions);

            _ = Task.Run(RunLocalFenProcessor);
            _ = Task.Run(RunOrientationProcessor);
            _ = Task.Run(RunRemoteFenProcessor);
            _ = Task.Run(RunClockProcessor);
            _ = Task.Run(RunLastMoveProcessor);
            _ = Task.Run(RunMessageProcessor);
        }

        public void NotifyInitComplete()
        {
            IsClientInitComplete = true;
            OnInitComplete?.Invoke();
        }

        public void PluginDisconnect()
        {
            OnPluginDisconnect?.Invoke();
        }

        public async Task WatchStateChange(CherubApiMessage.MessageTypeCode messageType, string remoteSource, BoardState remoteBoardState = null)
        {
            try
            {
                await startStopSemaphore.WaitAsync();

                if (messageType == MessageTypeCode.WATCH_STARTED)
                {
                    OnRemoteWatchStarted?.Invoke(remoteSource);
                }
                else if (messageType == MessageTypeCode.WATCH_STOPPED)
                {
                    RemoteBoardFEN = "";
                    OnRemoteWatchStopped?.Invoke(remoteSource);
                    OnRemoteDisconnect?.Invoke();
                }
                else if (messageType == MessageTypeCode.WATCH_STOPPED_MOVES_ONLY)
                {
                    OnRemoteWatchStopped?.Invoke(remoteSource);
                    OnRemoteDisconnect?.Invoke();
                }
            }
            finally { _ = startStopSemaphore.Release(); }
        }

        private bool remoteIgnored;

        private void KillRemoteConnections()
        {
            remoteIgnored = true;
            _ = WatchStateChange(MessageTypeCode.WATCH_STOPPED, "Kibitz Started");
            ResetRemoteBoardState();
        }

        public void LocalBoardUpdate(string fen)
        {
            Enqueue(localFenProcessChannel.Writer, fen);
        }

        /// <inheritdoc />
        public bool DesktopClientMode { get; set; } = true;

        public void RemoteBoardUpdated(BoardState remoteBoardState)
        {
            if (remoteIgnored)
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(remoteBoardState);

            //Always send these
            Enqueue(orientationProcessChannel.Writer, remoteBoardState.Board.IsWhiteOnBottom);
            Enqueue(clockProcessChannel.Writer, remoteBoardState);

            //...and then ignore if we already have the FEN
            if (RemoteBoardFEN != null && RemoteBoardFEN == remoteBoardState.Board.FenString)
            {
                return;
            }

            if (remoteBoardState.State.Code == ResponseCode.GAME_PENDING)
            {
                ResetRemoteBoardState();
            }

            remoteBoardState.Board.LastFenString = RemoteBoardFEN == null ? "" : RemoteBoardFEN.ToString();

            if (!string.IsNullOrWhiteSpace(remoteBoardState.Board.LastFenString))
            {
                (string move, string ending, string turn) = ChessHelpers.PositionDiffCalculator.CalculateSanFromFen(remoteBoardState.Board.LastFenString, remoteBoardState.Board.FenString);
                remoteBoardState.Board.LastMove = move;
                remoteBoardState.Board.Ending = ending;
                remoteBoardState.Board.FenTurn = turn == "WHITE" ? TurnCode.WHITE : turn == "BLACK" ? TurnCode.BLACK : TurnCode.UNKNOWN;
            }
            else
            {
                remoteBoardState.Board.LastMove = "";
            }

            Enqueue(remoteFenProcessChannel.Writer, remoteBoardState);


            if (!string.IsNullOrWhiteSpace(remoteBoardState.Board.LastMove))
            {
                Enqueue(lastMoveProcessChannel.Writer, remoteBoardState);
            }
        }

        public void UserMessageArrived(string source, string message)
        {
            if (remoteIgnored)
            {
                return;
            }

            Enqueue(messageProcessChannel.Writer, (source, message));
        }

        public void ResetLocalBoardState()
        {
            LocalBoardFEN = "";
            IsMismatchDetected = false;
        }

        private void ResetRemoteBoardState()
        {
            WhiteClock = "00:00";
            BlackClock = "00:00";
            RunWhoString = "0";

            OnClockChange?.Invoke();

            IsMismatchDetected = false;
            whiteNextClockAudioNotBefore = double.MaxValue;
            blackNextClockAudioNotBefore = double.MaxValue;

            RemoteBoardStatusMessage = "";
        }


        private async Task RunLocalFenProcessor()
        {
            while (true)
            {
                string fen = await localFenProcessChannel.Reader.ReadAsync();

                try
                {
                    await ApplyLocalFen(fen);
                }
                catch (Exception ex)
                {
                    // This loop is the only thing moving the local board forward. If it faults,
                    // LocalBoardFEN stops changing for the rest of the session - the board picture,
                    // every clock page and the engine all freeze on the last position, and nothing
                    // says why. One bad position must not be able to do that.
                    _logger?.LogError(ex, "Local board processor failed on [{Fen}]; continuing with the next position.", fen);
                }
                finally
                {
                    _ = System.Threading.Interlocked.Decrement(ref inFlight);
                }
            }
        }

        private async Task ApplyLocalFen(string fen)
        {
            if (string.IsNullOrWhiteSpace(fen) || LocalBoardFEN == fen) { return; }

            // The previous board is what the turn and en passant are inferred from, so it has to
            // be taken before LocalBoardFEN is replaced - after that the history is gone and every
            // position looks like the first one.
            string previousFen = LocalBoardFEN;

            LocalBoardFEN = fen;
            OnLocalFenChange?.Invoke(LocalBoardFEN);

            if (IsLocalBoardAvailable &&
                IsRemoteBoardAvailable)
            {
                // If the fens match we have caught up to the remote board.
                // Run the matcher straight away to clear any outstanding match requests.
                // There is no need to match after our moves - issues will be detected by the remote board match
                CurrentUpdatetMatch = Guid.NewGuid();
                _ = Task.Run(() => TestForBoardMatch(CurrentUpdatetMatch.ToString(),
                                   IsBoardInSync ? MatcherLocalDelayMs : FromMismatchDelayMs));
            }
            else if (IsLocalBoardAvailable &&
                      !IsRemoteBoardAvailable)
            {
                // Whose turn it is, what castling is still available and whether en passant is on
                // are inferred from the board now and the board before the last move. The board
                // itself knows only where the pieces are.
                //
                // This is local arithmetic and runs whether or not the kibitzer is on, so that
                // switching it on has a position to analyse straight away.
                KibitzerPosition position = KibitzerPosition.Infer(fen, previousFen);
                CurrentKibitzerPosition = position;
                OnKibitzerPositionChange?.Invoke(position);

                // The engine is only ever told about the board once the player has turned the
                // kibitzer on. A loaded engine that is not being fed positions cannot be used to
                // cheat, which is the whole point of the switch.
                if (_uciEngineManager != null && _uciEngineManager.IsKibitzerRunning)
                {
                    SendToEngine(position);
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(POST_EVENT_DELAY_LOCAL_FEN), TimeSource);
        }

        /// <summary>
        /// Sends the position on the board to the engine again.
        /// </summary>
        /// <remarks>
        /// A position is normally sent when the board changes, which means an engine loaded while
        /// the board is sitting still is never asked about the position in front of it - the
        /// kibitzer looks dead until the player moves a piece. Switching the kibitzer on, or
        /// loading an engine, calls this so the analysis starts straight away.
        /// </remarks>
        public void AnalyseCurrentPosition()
        {
            // Refused unless the kibitzer is on: an engine may be loaded, but it is not told
            // about the board until the player has declared the session is for analysis.
            if (_uciEngineManager == null || !_uciEngineManager.IsKibitzerRunning) { return; }

            KibitzerPosition position = CurrentKibitzerPosition;

            if (position != null) { SendToEngine(position); }
        }

        private void SendToEngine(KibitzerPosition position)
        {
            // The specification only allows a position command while the engine is idle, so end
            // the previous search first. The engine answers stop with bestmove and the next
            // position can be queued straight behind it.
            _uciEngineManager?.LoadedEngineStop();
            _uciEngineManager?.LoadedEngineSetPosition(position.Fen);
            _uciEngineManager?.LoadedEngineGoInfinite();
        }

        private async Task RunMessageProcessor()
        {
            while (true)
            {
                (string source, string message) message = await messageProcessChannel.Reader.ReadAsync();

                if (EchoExternalMessagesToConsole)
                {
                    OnNotification?.Invoke(message.source, message.message);
                    await Task.Delay(TimeSpan.FromMilliseconds(POST_EVENT_DELAY_MESSAGE), TimeSource);
                }

                _ = System.Threading.Interlocked.Decrement(ref inFlight);
            }
        }

        private async Task RunOrientationProcessor()
        {
            while (true)
            {
                bool isWhiteOnBottom = await orientationProcessChannel.Reader.ReadAsync();

                if (IsWhiteOnBottom != isWhiteOnBottom)
                {
                    IsWhiteOnBottom = isWhiteOnBottom;
                    OnOrientationFlipped?.Invoke();
                    await Task.Delay(TimeSpan.FromMilliseconds(POST_EVENT_DELAY_ORIENTATION), TimeSource);
                }

                _ = System.Threading.Interlocked.Decrement(ref inFlight);
            }
        }

        private async Task RunClockProcessor()
        {
            while (true)
            {
                BoardState remoteBoardState = await clockProcessChannel.Reader.ReadAsync();
                _logger?.LogTrace("Processing a clock recieved @ {CaptureTimeMs}", remoteBoardState.CaptureTimeMs);

                // Account for the actual time captured/now if clock running
                int captureTimeDiffMs = (int)(TimeSource.GetUtcNow().UtcDateTime.Subtract(DateTime.UnixEpoch).TotalMilliseconds - remoteBoardState.Board.Clocks.CaptureTimeMs);
                TimeSpan whiteTimespan = new(0, 0, 0, 0, remoteBoardState.Board.Clocks.WhiteClock - ((remoteBoardState.Board.ClockTurn == TurnCode.WHITE) ? captureTimeDiffMs : 0));
                TimeSpan blackTimespan = new(0, 0, 0, 0, remoteBoardState.Board.Clocks.BlackClock - ((remoteBoardState.Board.ClockTurn == TurnCode.BLACK) ? captureTimeDiffMs : 0));

                WhiteClockMsRemaining = (int)whiteTimespan.TotalMilliseconds;
                BlackClockMsRemaining = (int)blackTimespan.TotalMilliseconds;

                string whiteClockString = $"{whiteTimespan.Hours}:{whiteTimespan.Minutes.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0')}:{whiteTimespan.Seconds.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0')}";
                string blackClockString = $"{blackTimespan.Hours}:{blackTimespan.Minutes.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0')}:{blackTimespan.Seconds.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0')}";
                int runWho = remoteBoardState.Board.ClockTurn == TurnCode.WHITE ? 1 : remoteBoardState.Board.ClockTurn == TurnCode.BLACK ? 2 : 0;

                WhiteClock = whiteClockString;
                BlackClock = blackClockString;
                RunWhoString = runWho.ToString(CultureInfo.InvariantCulture);

                OnClockChange?.Invoke();
                CalculateNextClockAudio(WhiteClockMsRemaining, ref whiteNextClockAudioNotBefore, (string audioFile) => OnPlayWhiteClockAudio?.Invoke(audioFile));
                CalculateNextClockAudio(BlackClockMsRemaining, ref blackNextClockAudioNotBefore, (string audioFile) => OnPlayBlackClockAudio?.Invoke(audioFile));

                await Task.Delay(TimeSpan.FromMilliseconds(POST_EVENT_DELAY_CLOCK), TimeSource);

                _ = System.Threading.Interlocked.Decrement(ref inFlight);
            }
        }

        private async Task RunLastMoveProcessor()
        {
            while (true)
            {
                BoardState remoteBoardState = await lastMoveProcessChannel.Reader.ReadAsync();

                _logger?.LogTrace("Processing a move recieved @ {CaptureTimeMs}", remoteBoardState.CaptureTimeMs);

                if (LastMove is null ||
                    lastMoveVoiceTest != $"{remoteBoardState.Board.LastMove}{remoteBoardState.Board.FenString}")
                {
                    LastMove = remoteBoardState.Board.LastMove;
                    lastMoveVoiceTest = $"{remoteBoardState.Board.LastMove}{remoteBoardState.Board.FenString}";

                    OnNotification?.Invoke("LMOVE", $"New move detected '{remoteBoardState.Board.LastMove}'");

                    // If turncode is none then read all moves
                    bool isPlayerTurn = remoteBoardState.Board.FenTurn == TurnCode.NONE ||
                                          (IsWhiteOnBottom && remoteBoardState.Board.FenTurn != TurnCode.WHITE) ||
                                          (!IsWhiteOnBottom && remoteBoardState.Board.FenTurn != TurnCode.BLACK);

                    OnNewMoveDetected?.Invoke(LastMove, isPlayerTurn);

                    if (!string.IsNullOrWhiteSpace(remoteBoardState.Board.Ending))
                    {
                        if (remoteBoardState.Board.Ending is "1-0" or
                            "0-1" or
                            "1/2-1/2")
                        {
                            OnNewMoveDetected?.Invoke(remoteBoardState.Board.Ending, true);
                        }
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(POST_EVENT_DELAY_LAST_MOVE), TimeSource);
                }

                _ = System.Threading.Interlocked.Decrement(ref inFlight);
            }
        }

        private async Task RunRemoteFenProcessor()
        {
            while (true)
            {
                BoardState remoteBoardState = await remoteFenProcessChannel.Reader.ReadAsync();
                _logger?.LogTrace("Processing a board recieved @ {CaptureTimeMs}", remoteBoardState.CaptureTimeMs);


                if (RemoteBoardFEN != remoteBoardState.Board.FenString)
                {
                    _logger?.LogTrace($"FEN Change");

                    RemoteBoardFEN = remoteBoardState.Board.FenString;
                    FromRemoteBoardFEN = remoteBoardState.Board.LastFenString;
                    LastMove = remoteBoardState.Board.LastMove;

                    OnRemoteFenChange?.Invoke(FromRemoteBoardFEN, RemoteBoardFEN, LastMove,
                                              remoteBoardState.Board.ClockTurn.ToString(),
                                              remoteBoardState.Board.FenTurn.ToString(),
                                              remoteBoardState.BoardConnection.ConMessage,
                                              remoteBoardState.Board.IsWhiteOnBottom);

                    CurrentUpdatetMatch = Guid.NewGuid();
                    _ = Task.Run(() => TestForBoardMatch(CurrentUpdatetMatch.ToString(), MatcherRemoteTimeDelayMs));

                    await Task.Delay(TimeSpan.FromMilliseconds(POST_EVENT_DELAY_REMOTE_FEN), TimeSource);
                }

                if (RemoteBoardFEN != remoteBoardState.BoardConnection.ConMessage)
                {
                    RemoteBoardStatusMessage = remoteBoardState.BoardConnection.ConMessage;
                    OnRemoteBoardStatusChange?.Invoke(remoteBoardState.BoardConnection.ConMessage,
                                                     remoteBoardState.Board.IsWhiteOnBottom);
                }

                _ = System.Threading.Interlocked.Decrement(ref inFlight);
            }
        }

        private async Task TestForBoardMatch(string matchCode, int matchDelay)
        {
            if (IsLocalBoardAvailable && IsRemoteBoardAvailable)
            {
                OnBoardMatcherStarted?.Invoke();
                //IsBoardInSync = false;

                _logger?.LogTrace($"MATCHER PRE IN:{matchCode} OUT:{CurrentUpdatetMatch}");
                await Task.Delay(TimeSpan.FromMilliseconds(matchDelay), TimeSource);

                // The match code was captured when the method was called so compare to the outside value and
                // if they are not the same we can skip as the local position has changed.
                //lock (matcherLockObj)
                //{
                if (matchCode != CurrentUpdatetMatch.ToString())
                {
                    return;
                }
                //}


                _logger?.LogTrace($"MATCHER POST IN:{matchCode} OUT:{CurrentUpdatetMatch}");

                //lock (matcherLockObj)
                //{
                if (RemoteBoardFEN != LocalBoardFEN)
                {
                    IsBoardInSync = false;
                    OnBoardMissmatch?.Invoke(TimeSource.GetUtcNow().UtcTicks, FenConversion.SquareDiffCount(LocalBoardFEN, RemoteBoardFEN), LastMatchedPosition, LocalBoardFEN);
                }
                else
                {
                    LastMatchedPosition = LocalBoardFEN;

                    if (!IsBoardInSync)
                    {
                        IsBoardInSync = true;
                        OnBoardMatchFromMissmatch?.Invoke(TimeSource.GetUtcNow().UtcTicks);
                    }
                }
                //}
            }
            else
            {
                _logger?.LogTrace($"MATCHER CANX IN:{matchCode} OUT:{CurrentUpdatetMatch}");
            }

        }

        private static void CalculateNextClockAudio(double clockMs, ref double nextAudioNotBefore, Action<string> onPlayAudio)
        {
            TimeSpan clockAudioTs;
            if ((clockAudioTs = TimeSpan.FromMilliseconds(clockMs)).TotalMilliseconds < nextAudioNotBefore)
            {
                bool isFirstCall = nextAudioNotBefore == double.MaxValue;
                string audioFile = "";

                if (clockAudioTs.Hours > 0)
                {
                    nextAudioNotBefore = clockAudioTs.TotalMilliseconds - (((clockAudioTs.Hours - 1) * MS_IN_HOUR) + (clockAudioTs.Minutes * MS_IN_MIN) + (clockAudioTs.Seconds * MS_IN_SEC) + (clockAudioTs.Milliseconds % MS_IN_SEC));
                }
                else if (clockAudioTs.Minutes > 25)
                {
                    nextAudioNotBefore = clockAudioTs.TotalMilliseconds - ((clockAudioTs.Minutes % 5 * MS_IN_MIN) + (clockAudioTs.Seconds * MS_IN_SEC) + (clockAudioTs.Milliseconds % MS_IN_SEC));
                    audioFile = isFirstCall ? "" : $"M_{(clockAudioTs.Minutes + ((clockAudioTs.Seconds is > 45 or < 15) ? 1 : 0)).ToString(CultureInfo.InvariantCulture).PadLeft(2, '0')}";
                }
                else if (clockAudioTs.Minutes > 0)
                {
                    nextAudioNotBefore = clockAudioTs.TotalMilliseconds - ((clockAudioTs.Seconds * MS_IN_SEC) + (clockAudioTs.Milliseconds % MS_IN_SEC));
                    audioFile = isFirstCall ? "" : $"M_{(clockAudioTs.Minutes + ((clockAudioTs.Seconds is > 45 or < 15) ? 1 : 0)).ToString(CultureInfo.InvariantCulture).PadLeft(2, '0')}";
                }
                else if (clockAudioTs.Seconds > 30)
                {
                    nextAudioNotBefore = clockAudioTs.TotalMilliseconds - ((clockAudioTs.Seconds % 5 * MS_IN_SEC) + (clockAudioTs.Milliseconds % MS_IN_SEC));
                    audioFile = isFirstCall ? "" : $"S_{clockAudioTs.Seconds.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0')}";
                }
                else if (clockAudioTs.Seconds > 0)
                {
                    nextAudioNotBefore = clockAudioTs.TotalMilliseconds - (clockAudioTs.Milliseconds % MS_IN_SEC);
                    audioFile = isFirstCall ? "" : $"S_{clockAudioTs.Seconds.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0')}";
                }

                // Play the audio if required
                if (!string.IsNullOrEmpty(audioFile)) { onPlayAudio?.Invoke(audioFile); }
            }
        }


    }
}




