using DgtAngelShared.Json;
using DynamicBoard.Helpers;
using Microsoft.Extensions.Logging;
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
        public event Action<long, string> OnBoardMatch;
        public event Action<long> OnBoardMatchFromMissmatch;
        public event Action<string> OnRemoteWatchStarted;
        public event Action<string> OnRemoteWatchStopped;
        public event Action<string, bool> OnNewMoveDetected;
        public event Action<string> OnPlayWhiteClockAudio;
        public event Action<string> OnPlayBlackClockAudio;
        public event Action<string, string> OnNotification;
        public event Action OnPluginDisconnect;
        public event Action<UciChessEngine> OnUciEngineLoaded;
        public event Action<string> OnUciEngineReleased;
        public event Action<string> OnUciEngineStartError;
        public event Action OnKibitzerActivated;
        public event Action OnKibitzerDeactivated;
        public event Action<string> OnKibitzerFenChange;
        public event Action<UciEngineEval> OnBoardEvalChanged;

        public bool IsClientInitComplete { get; private set; } = false;
        public bool IsWhiteOnBottom { get; private set; } = true;
        public bool IsMismatchDetected { get; private set; } = false;
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
        //private readonly IDgtEbDllFacade _dgtEbDllFacade;

        private readonly SemaphoreSlim startStopSemaphore = new(1, 1);

        private readonly Channel<string> localFenProcessChannel;
        private readonly Channel<BoardState> remoteFenProcessChannel;
        private readonly Channel<BoardState> clockProcessChannel;
        private readonly Channel<BoardState> lastMoveProcessChannel;
        private readonly Channel<bool> orientationProcessChannel;
        private readonly Channel<(string source, string message)> messageProcessChannel;

        //private readonly object matcherLockObj = new();

        private UciChessEngine CurrentUciEngine { get; set; } = null;

        private double whiteNextClockAudioNotBefore = double.MaxValue;
        private double blackNextClockAudioNotBefore = double.MaxValue;

        private string lastMoveVoiceTest = "";
        private bool isKibitzerRunning = false;


        //public AngelHubService(ILogger<AngelHubService> logger, IDgtEbDllFacade dgtEbDllFacade)
        public AngelHubService(ILogger<AngelHubService> logger, IUciEngineManager uciEngineManager)
        {
            _logger = logger;
            _uciEngineManager = uciEngineManager;
            //_dgtEbDllFacade = dgtEbDllFacade;


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


        public void SwitchKibitzer(bool turnOn = false)
        {
            if (turnOn)
            {
                KillRemoteConnections();
                isKibitzerRunning = true;
                OnKibitzerActivated();
            }
            else
            {
                isKibitzerRunning = false;
                CurrentUciEngine?.Stop();
                OnKibitzerDeactivated?.Invoke();
            }
        }

        public async Task LoadEngineAsync(string exePath)
        {
            UciChessEngine engSlot1 = _uciEngineManager.GetEngine("KIB_ENG_SLOT_1");
            UciChessEngine engSlot2 = _uciEngineManager.GetEngine("KIB_ENG_SLOT_2");

            string slotKey = engSlot1 == null ? "KIB_ENG_SLOT_1" : "KIB_ENG_SLOT_2";
            string slotRemoveKey = engSlot1 == null ? "KIB_ENG_SLOT_2" : "KIB_ENG_SLOT_1";

            await _uciEngineManager.RegisterEngineAsync(slotKey, new FileInfo(exePath));

            UciChessEngine engineNew = _uciEngineManager.GetEngine(slotKey);
            UciChessEngine engineOld = _uciEngineManager.GetEngine(slotRemoveKey);

            try
            {
                await _uciEngineManager.StartEngineAsync(slotKey);

                if (engineNew.IsUciOk)
                {
                    _ = engineNew.WaitForReady();
                    //eng.SetDebug(false);
                }

                //CurrentUciEngine.OnBoardEvalChanged -= CurrentUciEngine_OnBoardEvalChanged;
                CurrentUciEngine = engineNew;
                OnUciEngineLoaded?.Invoke(engineNew);
                CurrentUciEngine.OnBoardEvalChanged += CurrentUciEngine_OnBoardEvalChanged;

                if (engineOld != null)
                {
                    OnUciEngineReleased?.Invoke($"{engineOld.EngineName} [{engineOld.EngineAuthor}]");
                    await _uciEngineManager.UnRegisterEngineAsync(slotRemoveKey);
                }
            }
            catch (Exception ex)
            {
                await _uciEngineManager.UnRegisterEngineAsync(slotKey);
                OnUciEngineStartError?.Invoke($"Failed to start engine :: {ex.Message} [{exePath}]");
            }
        }

        private void CurrentUciEngine_OnBoardEvalChanged(UciEngineEval eval)
        {
            //Echo this to clients so they are ambivalent of the underlying engine running 
            OnBoardEvalChanged?.Invoke(eval);
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

        private bool remoteIgnored = false;

        private void KillRemoteConnections()
        {
            remoteIgnored = true;
            _ = WatchStateChange(MessageTypeCode.WATCH_STOPPED, "Kibitz Started");
            ResetRemoteBoardState(true);
        }

        public void LocalBoardUpdate(string fen)
        {
            _ = localFenProcessChannel.Writer.TryWrite(fen);
        }

        public void RemoteBoardUpdated(BoardState remoteBoardState)
        {
            if (remoteIgnored)
            {
                return;
            }

            //Always send these
            _ = orientationProcessChannel.Writer.TryWrite(remoteBoardState.Board.IsWhiteOnBottom);
            _ = clockProcessChannel.Writer.TryWrite(remoteBoardState);

            //...and then ignore if we already have the FEN
            if (RemoteBoardFEN != null && RemoteBoardFEN == remoteBoardState.Board.FenString)
            {
                return;
            }

            if (remoteBoardState.State.Code == ResponseCode.GAME_PENDING)
            {
                ResetRemoteBoardState(true);
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

            _ = remoteFenProcessChannel.Writer.TryWrite(remoteBoardState);


            if (!string.IsNullOrWhiteSpace(remoteBoardState.Board.LastMove))
            {
                _ = lastMoveProcessChannel.Writer.TryWrite(remoteBoardState);
            }
        }

        public void UserMessageArrived(string source, string message)
        {
            if (remoteIgnored)
            {
                return;
            }

            _ = messageProcessChannel.Writer.TryWrite((source, message));
        }

        public void ResetLocalBoardState()
        {
            LocalBoardFEN = "";
            IsMismatchDetected = false;
        }

        private void ResetRemoteBoardState(bool isGameCompleted = false)
        {
            //if (string.IsNullOrEmpty(RemoteBoardFEN = isGameCompleted ? RemoteBoardFEN : ""))
            //{
            //    OnRemoteDisconnect?.Invoke();
            //}

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

                if (!string.IsNullOrWhiteSpace(fen) && LocalBoardFEN != fen)
                {
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
                              !IsRemoteBoardAvailable &&
                              isKibitzerRunning)
                    {

                        char[] clearedflatFenArray = ChessHelpers.FenInference.FenToCharArray(fen);
                        int whiteCount = clearedflatFenArray.Count(char.IsUpper);
                        int blackCount = clearedflatFenArray.Count(char.IsLower);


                        string halfMoveClock = "0";
                        string fullMoveNumber = "1";


                        //promotesTo = enPass == "-" && isPawnMove && !string.IsNullOrEmpty(tmpPromotesTo) ? tmpPromotesTo : "";

                        bool isWhiteToPlay = false;

                        if (whiteCount > kibitzLastWhiteCount)
                        {
                            isWhiteToPlay = false;
                        }
                        else if (blackCount > kibitzLastBlackCount)
                        {
                            isWhiteToPlay = true;

                        }

                        string enPass = "-";
                        string castle = $"{((clearedflatFenArray[63] == 'R' && clearedflatFenArray[60] == 'K') ? "K" : "")}" +
                                        $"{((clearedflatFenArray[56] == 'R' && clearedflatFenArray[60] == 'K') ? "Q" : "")}" +
                                        $"{((clearedflatFenArray[7] == 'r' && clearedflatFenArray[4] == 'k') ? "k" : "")}" +
                                        $"{((clearedflatFenArray[0] == 'r' && clearedflatFenArray[4] == 'k') ? "q" : "")}";

                        string inferredFenTailFromPosition = $" {(isWhiteToPlay ? "w" : "b")} {(string.IsNullOrEmpty(castle) ? "-" : castle)} {enPass} {halfMoveClock} {fullMoveNumber}";
                        string joinedFen = $"{fen} {inferredFenTailFromPosition}";

                        CurrentUciEngine?.Stop();
                        OnKibitzerFenChange?.Invoke(joinedFen);

                        CurrentUciEngine?.SetPosition(joinedFen);
                        CurrentUciEngine?.GoInfinite();

                        kibitzClearedflatFenArray = clearedflatFenArray;
                        kibitzLastWhiteCount = whiteCount;
                        kibitzLastBlackCount = blackCount;
                        lastLocalBoardFenForKibitzer = fen;
                    }

                    await Task.Delay(POST_EVENT_DELAY_LOCAL_FEN);
                }
            }
        }

        private char[] kibitzClearedflatFenArray = Array.Empty<char>();
        private int kibitzLastWhiteCount = 0;
        private int kibitzLastBlackCount = 0;
        private string lastLocalBoardFenForKibitzer = "";
        private readonly TurnCode kibturn;
        private readonly string kibend = "";
        private readonly string kibmove = "";

        private async Task RunMessageProcessor()
        {
            while (true)
            {
                (string source, string message) message = await messageProcessChannel.Reader.ReadAsync();

                if (EchoExternalMessagesToConsole)
                {
                    OnNotification?.Invoke(message.source, message.message);
                    await Task.Delay(POST_EVENT_DELAY_MESSAGE);
                }
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
                    await Task.Delay(POST_EVENT_DELAY_ORIENTATION);
                }
            }
        }

        private async Task RunClockProcessor()
        {
            while (true)
            {
                BoardState remoteBoardState = await clockProcessChannel.Reader.ReadAsync();
                _logger?.LogTrace("Processing a clock recieved @ {CaptureTimeMs}", remoteBoardState.CaptureTimeMs);

                // Account for the actual time captured/now if clock running
                int captureTimeDiffMs = (int)(DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds - remoteBoardState.Board.Clocks.CaptureTimeMs);
                TimeSpan whiteTimespan = new(0, 0, 0, 0, remoteBoardState.Board.Clocks.WhiteClock - ((remoteBoardState.Board.ClockTurn == TurnCode.WHITE) ? captureTimeDiffMs : 0));
                TimeSpan blackTimespan = new(0, 0, 0, 0, remoteBoardState.Board.Clocks.BlackClock - ((remoteBoardState.Board.ClockTurn == TurnCode.BLACK) ? captureTimeDiffMs : 0));

                WhiteClockMsRemaining = (int)whiteTimespan.TotalMilliseconds;
                BlackClockMsRemaining = (int)blackTimespan.TotalMilliseconds;

                string whiteClockString = $"{whiteTimespan.Hours}:{whiteTimespan.Minutes.ToString().PadLeft(2, '0')}:{whiteTimespan.Seconds.ToString().PadLeft(2, '0')}";
                string blackClockString = $"{blackTimespan.Hours}:{blackTimespan.Minutes.ToString().PadLeft(2, '0')}:{blackTimespan.Seconds.ToString().PadLeft(2, '0')}";
                int runWho = remoteBoardState.Board.ClockTurn == TurnCode.WHITE ? 1 : remoteBoardState.Board.ClockTurn == TurnCode.BLACK ? 2 : 0;

                WhiteClock = whiteClockString;
                BlackClock = blackClockString;
                RunWhoString = runWho.ToString();

                OnClockChange?.Invoke();
                CalculateNextClockAudio(WhiteClockMsRemaining, ref whiteNextClockAudioNotBefore, (string audioFile) => OnPlayWhiteClockAudio?.Invoke(audioFile));
                CalculateNextClockAudio(BlackClockMsRemaining, ref blackNextClockAudioNotBefore, (string audioFile) => OnPlayBlackClockAudio?.Invoke(audioFile));

                await Task.Delay(POST_EVENT_DELAY_CLOCK);
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

                    await Task.Delay(POST_EVENT_DELAY_LAST_MOVE);
                }
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

                    await Task.Delay(POST_EVENT_DELAY_REMOTE_FEN);
                }

                if (RemoteBoardFEN != remoteBoardState.BoardConnection.ConMessage)
                {
                    RemoteBoardStatusMessage = remoteBoardState.BoardConnection.ConMessage;
                    OnRemoteBoardStatusChange?.Invoke(remoteBoardState.BoardConnection.ConMessage,
                                                     remoteBoardState.Board.IsWhiteOnBottom);
                }
            }
        }

        private async Task TestForBoardMatch(string matchCode, int matchDelay)
        {
            if (IsLocalBoardAvailable && IsRemoteBoardAvailable)
            {
                OnBoardMatcherStarted?.Invoke();
                //IsBoardInSync = false;

                _logger?.LogTrace("MATCHER", $"PRE IN:{matchCode} OUT:{CurrentUpdatetMatch}");
                await Task.Delay(matchDelay);

                // The match code was captured when the method was called so compare to the outside value and
                // if they are not the same we can skip as the local position has changed.
                //lock (matcherLockObj)
                //{
                if (matchCode != CurrentUpdatetMatch.ToString())
                {
                    return;
                }
                //}


                _logger?.LogTrace("POST IN OUT", $"IN:{matchCode} OUT:{CurrentUpdatetMatch}");

                //lock (matcherLockObj)
                //{
                if (RemoteBoardFEN != LocalBoardFEN)
                {
                    IsBoardInSync = false;
                    OnBoardMissmatch?.Invoke(DateTime.UtcNow.Ticks, FenConversion.SquareDiffCount(LocalBoardFEN, RemoteBoardFEN), LastMatchedPosition, LocalBoardFEN);
                }
                else
                {
                    LastMatchedPosition = LocalBoardFEN;
                    //OnBoardMatch?.Invoke(LocalBoardFEN);

                    if (!IsBoardInSync)
                    {
                        IsBoardInSync = true;
                        OnBoardMatchFromMissmatch?.Invoke(DateTime.UtcNow.Ticks);
                    }
                }
                //}
            }
            else
            {
                _logger?.LogTrace("CANX IN OUT", $"IN:{matchCode} OUT:{CurrentUpdatetMatch}");
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
                    audioFile = isFirstCall ? "" : $"M_{(clockAudioTs.Minutes + ((clockAudioTs.Seconds is > 45 or < 15) ? 1 : 0)).ToString().PadLeft(2, '0')}";
                }
                else if (clockAudioTs.Minutes > 0)
                {
                    nextAudioNotBefore = clockAudioTs.TotalMilliseconds - ((clockAudioTs.Seconds * MS_IN_SEC) + (clockAudioTs.Milliseconds % MS_IN_SEC));
                    audioFile = isFirstCall ? "" : $"M_{(clockAudioTs.Minutes + ((clockAudioTs.Seconds is > 45 or < 15) ? 1 : 0)).ToString().PadLeft(2, '0')}";
                }
                else if (clockAudioTs.Seconds > 30)
                {
                    nextAudioNotBefore = clockAudioTs.TotalMilliseconds - ((clockAudioTs.Seconds % 5 * MS_IN_SEC) + (clockAudioTs.Milliseconds % MS_IN_SEC));
                    audioFile = isFirstCall ? "" : $"S_{clockAudioTs.Seconds.ToString().PadLeft(2, '0')}";
                }
                else if (clockAudioTs.Seconds > 0)
                {
                    nextAudioNotBefore = clockAudioTs.TotalMilliseconds - (clockAudioTs.Milliseconds % MS_IN_SEC);
                    audioFile = isFirstCall ? "" : $"S_{clockAudioTs.Seconds.ToString().PadLeft(2, '0')}";
                }

                // Play the audio if required
                if (!string.IsNullOrEmpty(audioFile)) { onPlayAudio?.Invoke(audioFile); }
            }
        }


    }
}




