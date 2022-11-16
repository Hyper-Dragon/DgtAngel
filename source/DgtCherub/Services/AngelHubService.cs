using DgtAngelShared.Json;
using DgtEbDllWrapper;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using DynamicBoard.Helpers;
using static DgtAngelShared.Json.CherubApiMessage;

namespace DgtCherub.Services
{
    public interface IAngelHubService
    {
        string BlackClock { get; }
        int BlackClockMsRemaining { get; }
        string RemoteBoardFEN { get; }
        bool EchoExternalMessagesToConsole { get; }
        bool IsBoardInSync { get; }
        bool IsRemoteBoardStateActive { get; }
        bool IsLocalBoardAvailable { get; }
        bool IsMismatchDetected { get; }
        bool IsRemoteBoardAvailable { get; }
        bool IsWhiteOnBottom { get; }
        string LastMove { get; }
        string LocalBoardFEN { get; }
        string RunWhoString { get; }
        string WhiteClock { get; }
        int WhiteClockMsRemaining { get; }

        public int MatcherRemoteTimeDelayMs { get; set; }

        event Action<string> OnBoardMatch;
        event Action OnBoardMatcherStarted;
        event Action OnBoardMatchFromMissmatch;
        event Action<int,string,string> OnBoardMissmatch;
        event Action OnRemoteDisconnect;
        event Action OnClockChange;
        event Action<string> OnLocalFenChange;
        event Action OnRemoteWatchStarted;
        event Action OnRemoteWatchStopped;
        event Action<string, bool> OnNewMoveDetected;
        event Action OnOrientationFlipped;
        event Action<string> OnPlayBlackClockAudio;
        event Action<string> OnPlayWhiteClockAudio;
        event Action<string, string, string> OnRemoteFenChange;
        event Action<string, string> OnNotification;

        void LocalBoardUpdate(string fen);
        void RemoteBoardUpdated(BoardState remoteBoardState);
        void ResetLocalBoardState();
        void UserMessageArrived(string source, string message);
        void WatchStateChange(MessageTypeCode messageType, BoardState remoteBoardState = null);
    }

    public sealed class AngelHubService : IAngelHubService
    {
        public event Action<string> OnLocalFenChange;
        public event Action<string, string,string> OnRemoteFenChange;
        public event Action OnRemoteDisconnect;
        public event Action OnClockChange;
        public event Action OnOrientationFlipped;
        public event Action<int,string,string> OnBoardMissmatch;
        public event Action OnBoardMatcherStarted;
        public event Action<string> OnBoardMatch;
        public event Action OnBoardMatchFromMissmatch;
        public event Action OnRemoteWatchStarted;
        public event Action OnRemoteWatchStopped;
        public event Action<string, bool> OnNewMoveDetected;
        public event Action<string> OnPlayWhiteClockAudio;
        public event Action<string> OnPlayBlackClockAudio;
        public event Action<string, string> OnNotification;

        public bool IsWhiteOnBottom { get; private set; } = true;
        public bool IsMismatchDetected { get; private set; } = false;
        public bool EchoExternalMessagesToConsole { get; private set; } = true;
        public string LocalBoardFEN { get; private set; }
        public string LastMatchedPosition { get; private set; }
        public string RemoteBoardFEN { get; private set; }
        public string FromRemoteBoardFEN { get; private set; }
        public string LastMove { get; private set; }
        public int WhiteClockMsRemaining { get; private set; }
        public int BlackClockMsRemaining { get; private set; }
        public string WhiteClock { get; private set; } = "00:00";
        public string BlackClock { get; private set; } = "00:00";
        public string RunWhoString { get; private set; } = "0";
        public int MatcherRemoteTimeDelayMs { get; set; } = MATCHER_REMOTE_TIME_DELAY_MS;
        public bool IsLocalBoardAvailable => !string.IsNullOrWhiteSpace(LocalBoardFEN);
        public bool IsRemoteBoardAvailable => !string.IsNullOrWhiteSpace(RemoteBoardFEN);
        public bool IsBoardInSync { get; private set; } = true;
        public bool IsRemoteBoardStateActive => (RemoteBoardFEN != "" && WhiteClock != "00:00") || BlackClock != "00:00";
        private Guid CurrentUpdatetMatch { get; set; } = Guid.NewGuid();


        private const int MS_IN_HOUR = 3600000;
        private const int MS_IN_MIN = 60000;
        private const int MS_IN_SEC = 1000;

        private const int MATCHER_REMOTE_TIME_DELAY_MS = 5000;
        private const int MATCHER_LOCAL_TIME_DELAY_MS = 300;

        private const int POST_EVENT_DELAY_LAST_MOVE = MS_IN_SEC;
        private const int POST_EVENT_DELAY_LOCAL_FEN = MATCHER_LOCAL_TIME_DELAY_MS*2;
        private const int POST_EVENT_DELAY_REMOTE_FEN = MS_IN_SEC / 10;
        private const int POST_EVENT_DELAY_CLOCK = MS_IN_SEC / 2;
        private const int POST_EVENT_DELAY_MESSAGE = MS_IN_SEC / 10;
        private const int POST_EVENT_DELAY_ORIENTATION = MS_IN_SEC / 10;

        private readonly ILogger _logger;
        private readonly IDgtEbDllFacade _dgtEbDllFacade;

        private readonly SemaphoreSlim startStopSemaphore = new(1, 1);

        private readonly Channel<string> localFenProcessChannel;
        private readonly Channel<BoardState> remoteFenProcessChannel;
        private readonly Channel<BoardState> clockProcessChannel;
        private readonly Channel<BoardState> lastMoveProcessChannel;
        private readonly Channel<bool> orientationProcessChannel;
        private readonly Channel<(string source, string message)> messageProcessChannel;

        private double whiteNextClockAudioNotBefore = double.MaxValue;
        private double blackNextClockAudioNotBefore = double.MaxValue;

        private string lastMoveVoiceTest = "";
        
        public AngelHubService(ILogger<AngelHubService> logger, IDgtEbDllFacade dgtEbDllFacade)
        {
            _logger = logger;
            _dgtEbDllFacade = dgtEbDllFacade;


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

        public async void WatchStateChange(CherubApiMessage.MessageTypeCode messageType, BoardState remoteBoardState = null)
        {
            try
            {
                await startStopSemaphore.WaitAsync();

                if (messageType == MessageTypeCode.WATCH_STARTED)
                {
                    OnRemoteWatchStarted?.Invoke();
                }
                else if (messageType == MessageTypeCode.WATCH_STOPPED)
                {
                    OnRemoteWatchStopped?.Invoke();
                    ResetRemoteBoardState();
                }
                else if (messageType == MessageTypeCode.WATCH_STOPPED_MOVES_ONLY)
                {
                    OnRemoteWatchStopped?.Invoke();
                }
            }
            finally { _ = startStopSemaphore.Release(); }
        }


        public void LocalBoardUpdate(string fen)
        {
            _ = localFenProcessChannel.Writer.TryWrite(fen);
        }

        

        public void RemoteBoardUpdated(BoardState remoteBoardState)
        {
            _ = orientationProcessChannel.Writer.TryWrite(remoteBoardState.Board.IsWhiteOnBottom);

            //Ignore if we already have the FEN
            if (this.RemoteBoardFEN != null && this.RemoteBoardFEN == remoteBoardState.Board.FenString) return;

            remoteBoardState.Board.LastFenString = this.RemoteBoardFEN == null ? "" : this.RemoteBoardFEN.ToString();



            if (!string.IsNullOrWhiteSpace(remoteBoardState.Board.LastFenString))
            {
                var (move, ending, turn) = ChessHelpers.PositionDiffCalculator.CalculateSanFromFen(remoteBoardState.Board.LastFenString, remoteBoardState.Board.FenString);
                remoteBoardState.Board.LastMove = move;
                remoteBoardState.Board.Ending = ending;
                remoteBoardState.Board.FenTurn = turn;
            }
            else
            {
                remoteBoardState.Board.LastMove = "";
            }

            if (remoteBoardState.State.Code == ResponseCode.GAME_IN_PROGRESS)
            {
                
                _ = remoteFenProcessChannel.Writer.TryWrite(remoteBoardState);
                _ = clockProcessChannel.Writer.TryWrite(remoteBoardState);

                if (!string.IsNullOrWhiteSpace(remoteBoardState.Board.LastMove))
                {
                    _ = lastMoveProcessChannel.Writer.TryWrite(remoteBoardState);
                }
            }

            if (!string.IsNullOrWhiteSpace(remoteBoardState.Board.Ending))
            {
                remoteBoardState.State.Code = ResponseCode.GAME_COMPLETED;
                remoteBoardState.Board.LastMove = remoteBoardState.Board.Ending;
            }

            if (remoteBoardState.State.Code == ResponseCode.GAME_COMPLETED)
            {
                if (remoteBoardState.Board.LastMove is "1-0" or
                    "0-1" or
                    "1/2-1/2")
                {
                    //ResetRemoteBoardState(true);
                    _ = lastMoveProcessChannel.Writer.TryWrite(remoteBoardState);
                    _ = orientationProcessChannel.Writer.TryWrite(remoteBoardState.Board.IsWhiteOnBottom);
                    _ = remoteFenProcessChannel.Writer.TryWrite(remoteBoardState);
                    _ = clockProcessChannel.Writer.TryWrite(remoteBoardState);
                }
            }
            else if (remoteBoardState.State.Code == ResponseCode.GAME_PENDING)
            {
                //ResetRemoteBoardState(true);
                _ = orientationProcessChannel.Writer.TryWrite(remoteBoardState.Board.IsWhiteOnBottom);
                _ = remoteFenProcessChannel.Writer.TryWrite(remoteBoardState);
            }
        }

        public void UserMessageArrived(string source, string message)
        {
            _ = messageProcessChannel.Writer.TryWrite((source, message));
        }

        public void ResetLocalBoardState()
        {
            LocalBoardFEN = "";
            IsMismatchDetected = false;
        }

        private void ResetRemoteBoardState(bool isGameCompleted = false)
        {
            if (string.IsNullOrEmpty(RemoteBoardFEN = isGameCompleted ? RemoteBoardFEN : ""))
            {
                OnRemoteDisconnect?.Invoke();
            }

            WhiteClock = "00:00";
            BlackClock = "00:00";
            RunWhoString = "0";

            OnClockChange?.Invoke();

            IsMismatchDetected = false;
            whiteNextClockAudioNotBefore = double.MaxValue;
            blackNextClockAudioNotBefore = double.MaxValue;
        }


        private async void RunLocalFenProcessor()
        {
            while (true)
            {
                string fen = await localFenProcessChannel.Reader.ReadAsync();

                if (!string.IsNullOrWhiteSpace(fen) && LocalBoardFEN != fen)
                {
                    LocalBoardFEN = fen;

                    //if (!IsBoardInSync && LocalBoardFEN == RemoteBoardFEN)
                    //{
                    if(IsLocalBoardAvailable && IsRemoteBoardAvailable) { 
                        // If the fens match we have caught up to the remote board.
                        // Run the matcher straight away to clear any outstanding match requests.
                        // There is no need to match after our moves - issues will be detected by the remote board match
                        CurrentUpdatetMatch = Guid.NewGuid();
                        _ = Task.Run(() => TestForBoardMatch(CurrentUpdatetMatch.ToString(), MATCHER_LOCAL_TIME_DELAY_MS));
                    }

                    OnLocalFenChange?.Invoke(LocalBoardFEN);
                    await Task.Delay(POST_EVENT_DELAY_LOCAL_FEN);
                }
            }
        }

        private async void RunMessageProcessor()
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

        private async void RunOrientationProcessor()
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

        private async void RunClockProcessor()
        {
            while (true)
            {
                BoardState remoteBoardState = await clockProcessChannel.Reader.ReadAsync();
                _logger?.LogTrace("Processing a clock recieved @ {CaptureTimeMs}", remoteBoardState.CaptureTimeMs);

                // Account for the actual time captured/now if clock running
                int captureTimeDiffMs = (int)(DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds - remoteBoardState.Board.Clocks.CaptureTimeMs);
                TimeSpan whiteTimespan = new(0, 0, 0, 0, remoteBoardState.Board.Clocks.WhiteClock - ((remoteBoardState.Board.Turn == TurnCode.WHITE) ? captureTimeDiffMs : 0));
                TimeSpan blackTimespan = new(0, 0, 0, 0, remoteBoardState.Board.Clocks.BlackClock - ((remoteBoardState.Board.Turn == TurnCode.BLACK) ? captureTimeDiffMs : 0));

                WhiteClockMsRemaining = (int)whiteTimespan.TotalMilliseconds;
                BlackClockMsRemaining = (int)blackTimespan.TotalMilliseconds;

                string whiteClockString = $"{whiteTimespan.Hours}:{whiteTimespan.Minutes.ToString().PadLeft(2, '0')}:{whiteTimespan.Seconds.ToString().PadLeft(2, '0')}";
                string blackClockString = $"{blackTimespan.Hours}:{blackTimespan.Minutes.ToString().PadLeft(2, '0')}:{blackTimespan.Seconds.ToString().PadLeft(2, '0')}";
                int runWho = remoteBoardState.Board.Turn == TurnCode.WHITE ? 1 : remoteBoardState.Board.Turn == TurnCode.BLACK ? 2 : 0;

                WhiteClock = whiteClockString;
                BlackClock = blackClockString;
                RunWhoString = runWho.ToString();

                OnClockChange?.Invoke();
                CalculateNextClockAudio(WhiteClockMsRemaining, ref whiteNextClockAudioNotBefore, (string audioFile) => OnPlayWhiteClockAudio?.Invoke(audioFile));
                CalculateNextClockAudio(BlackClockMsRemaining, ref blackNextClockAudioNotBefore, (string audioFile) => OnPlayBlackClockAudio?.Invoke(audioFile));

                await Task.Delay(POST_EVENT_DELAY_CLOCK);
            }
        }

        private async void RunLastMoveProcessor()
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


                    bool isPlayerTurn = ((IsWhiteOnBottom && remoteBoardState.Board.Turn != TurnCode.WHITE) ||
                                         (!IsWhiteOnBottom && remoteBoardState.Board.Turn != TurnCode.BLACK));

                    OnNewMoveDetected?.Invoke(LastMove, isPlayerTurn);
                    await Task.Delay(POST_EVENT_DELAY_LAST_MOVE);
                }
            }
        }

        private async void RunRemoteFenProcessor()
        {
            while (true)
            {
                BoardState remoteBoardState = await remoteFenProcessChannel.Reader.ReadAsync();
                _logger?.LogTrace("Processing a board recieved @ {CaptureTimeMs}", remoteBoardState.CaptureTimeMs);

                if (RemoteBoardFEN != remoteBoardState.Board.FenString)
                {
                    _logger?.LogTrace($"FEN Change");

                    //_dgtEbDllFacade.SetClock(whiteClockString, blackClockString, runWho);
                    RemoteBoardFEN = remoteBoardState.Board.FenString;
                    FromRemoteBoardFEN = remoteBoardState.Board.LastFenString;
                    LastMove = remoteBoardState.Board.LastMove;
                    
                    CurrentUpdatetMatch = Guid.NewGuid();
                    _ = Task.Run(() => TestForBoardMatch(CurrentUpdatetMatch.ToString(), MatcherRemoteTimeDelayMs));

                    OnRemoteFenChange?.Invoke(FromRemoteBoardFEN, RemoteBoardFEN, LastMove);
                    await Task.Delay(POST_EVENT_DELAY_REMOTE_FEN);
                }
            }
        }

        
        private async void TestForBoardMatch(string matchCode, int matchDelay)
        {
            if (IsLocalBoardAvailable && IsRemoteBoardAvailable)
            {
                OnBoardMatcherStarted?.Invoke();
                //IsBoardInSync = false;

                _logger?.LogTrace("MATCHER", $"PRE IN:{matchCode} OUT:{CurrentUpdatetMatch}");
                await Task.Delay(matchDelay);

                // The match code was captured when the method was called so compare to the outside value and
                // if they are not the same we can skip as the local position has changed.
                if (matchCode == CurrentUpdatetMatch.ToString())
                {
                    _logger?.LogTrace("POST IN OUT", $"IN:{matchCode} OUT:{CurrentUpdatetMatch}");

                    if (RemoteBoardFEN != LocalBoardFEN)
                    {
                        IsBoardInSync = false;
                        OnBoardMissmatch?.Invoke(FenConversion.SquareDiffCount(LocalBoardFEN,RemoteBoardFEN),LastMatchedPosition,LocalBoardFEN);
                    }
                    else
                    {
                        LastMatchedPosition = LocalBoardFEN;
                        OnBoardMatch?.Invoke(LocalBoardFEN);

                        if (!IsBoardInSync)
                        {  
                            IsBoardInSync = true;
                            OnBoardMatchFromMissmatch?.Invoke();
                        }
                    }
                }
                else
                {
                    _logger?.LogTrace("CANX IN OUT", $"IN:{matchCode} OUT:{CurrentUpdatetMatch}");
                }
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




