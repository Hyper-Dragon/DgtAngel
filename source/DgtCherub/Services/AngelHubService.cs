using DgtAngelShared.Json;
using DgtEbDllWrapper;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static DgtAngelShared.Json.CherubApiMessage;

namespace DgtCherub.Services
{
    public interface IAngelHubService
    {
        string BlackClock { get; }
        int BlackClockMs { get; }
        string ChessDotComBoardFEN { get; }
        bool EchoExternalMessagesToConsole { get; }
        bool IsBoardInSync { get; }
        bool IsChessDotComBoardStateActive { get; }
        bool IsLocalBoardAvailable { get; }
        bool IsMismatchDetected { get; }
        bool IsRemoteBoardAvailable { get; }
        bool IsWhiteOnBottom { get; }
        string LastMove { get; }
        string LocalBoardFEN { get; }
        string RunWhoString { get; }
        string WhiteClock { get; }
        int WhiteClockMs { get; }

        event Action OnBoardMatch;
        event Action OnBoardMatcherStarted;
        event Action OnBoardMatchFromMissmatch;
        event Action OnBoardMissmatch;
        event Action OnRemoteDisconnect;
        event Action OnClockChange;
        event Action OnLocalFenChange;
        event Action<string> OnNewMoveDetected;
        event Action OnOrientationFlipped;
        event Action<string> OnPlayBlackClockAudio;
        event Action<string> OnPlayWhiteClockAudio;
        event Action OnRemoteFenChange;
        event Action<string, string> OnNotification;

        void LocalBoardUpdate(string fen);
        void RemoteBoardUpdated(BoardState remoteBoardState);
        void ResetLocalBoardState();
        void RunClockProcessor();
        void RunFenProcessor();
        void RunLastMoveProcessor();
        void SetClocksStrings(string chessDotComWhiteClock, string chessDotComBlackClock, string chessDotComRunWhoString);
        void UserMessageArrived(string source, string message);
        void WatchStateChange(MessageTypeCode messageType, BoardState remoteBoardState = null);
    }

    public sealed class AngelHubService : IAngelHubService
    {
        private const int MATCHER_TIME_DELAY_MS = 4000;

        public event Action OnLocalFenChange;
        public event Action OnRemoteFenChange;
        public event Action OnRemoteDisconnect;
        public event Action OnClockChange;
        public event Action OnOrientationFlipped;
        public event Action OnBoardMissmatch;
        public event Action OnBoardMatcherStarted;
        public event Action OnBoardMatch;
        public event Action OnBoardMatchFromMissmatch;
        public event Action<string> OnNewMoveDetected;
        public event Action<string> OnPlayWhiteClockAudio;
        public event Action<string> OnPlayBlackClockAudio;
        public event Action<string, string> OnNotification;

        private string _chessDotComWhiteClock = "00:00";
        private string _chessDotComBlackClock = "00:00";
        private string _chessDotComRunWhoString = "0";

        public bool IsWhiteOnBottom { get; private set; } = true;
        public bool IsMismatchDetected { get; private set; } = false;
        public bool EchoExternalMessagesToConsole { get; private set; } = true;
        public string LocalBoardFEN { get; private set; }
        public string ChessDotComBoardFEN { get; private set; }
        public string LastMove { get; private set; }
        public int WhiteClockMs { get; private set; }
        public int BlackClockMs { get; private set; }
        public string WhiteClock => _chessDotComWhiteClock;
        public string BlackClock => _chessDotComBlackClock;
        public string RunWhoString => _chessDotComRunWhoString;
        public bool IsLocalBoardAvailable => (!string.IsNullOrWhiteSpace(LocalBoardFEN));
        public bool IsRemoteBoardAvailable => (!string.IsNullOrWhiteSpace(ChessDotComBoardFEN));
        public bool IsBoardInSync { get; private set; } = true;
        public bool IsChessDotComBoardStateActive => (ChessDotComBoardFEN != "" && _chessDotComWhiteClock != "00:00" || _chessDotComBlackClock != "00:00");
        private Guid CurrentUpdatetMatch { get; set; } = Guid.NewGuid();

        private readonly ILogger _logger;
        private readonly IDgtEbDllFacade _dgtEbDllFacade;

        private readonly SemaphoreSlim startStopSemaphore = new(1, 1);
        private volatile bool processUpdates = false;

        private readonly Channel<BoardState> fenProcessChannel;
        private readonly Channel<BoardState> clockProcessChannel;
        private readonly Channel<BoardState> lastMoveProcessChannel;
        private readonly Channel<(string source, string message)> messageProcessChannel;

        private double whiteNextClockAudioNotBefore = double.MaxValue;
        private double blackNextClockAudioNotBefore = double.MaxValue;

        public AngelHubService(ILogger<AngelHubService> logger, IDgtEbDllFacade dgtEbDllFacade)
        {
            _logger = logger;
            _dgtEbDllFacade = dgtEbDllFacade;

            BoundedChannelOptions processChannelOptions = new(1)
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
            fenProcessChannel = Channel.CreateBounded<BoardState>(processChannelOptions);
            clockProcessChannel = Channel.CreateBounded<BoardState>(processChannelOptions);
            lastMoveProcessChannel = Channel.CreateBounded<BoardState>(processChannelOptions);
            messageProcessChannel = Channel.CreateBounded<(string source, string message)>(messageChannelOptions);

            Task.Run(() => RunFenProcessor());
            Task.Run(() => RunClockProcessor());
            Task.Run(() => RunLastMoveProcessor());
            Task.Run(() => RunMessageProcessor());
        }

        private async void TestForBoardMatch(string matchCode)
        {
            if (IsLocalBoardAvailable && IsRemoteBoardAvailable)
            {
                OnBoardMatcherStarted?.Invoke();

                //OnUserMessageArrived("MATCHER", $"PRE  IN:{matchCode} OUT:{CurrentUpdatetMatch}");
                await Task.Delay(MATCHER_TIME_DELAY_MS);

                // The match code was captured when the method was called so compare to the outside value and
                // if they are not the same we can skip as the local position has changed.
                if (matchCode == CurrentUpdatetMatch.ToString())
                {
                    OnNotification("MATCHER", $"POST IN:{matchCode} OUT:{CurrentUpdatetMatch}");

                    if (ChessDotComBoardFEN != LocalBoardFEN)
                    {
                        IsBoardInSync = false;
                        OnBoardMissmatch?.Invoke();
                    }
                    else
                    {
                        OnBoardMatch?.Invoke();

                        if (!IsBoardInSync)
                        {
                            IsBoardInSync = true;
                            OnBoardMatchFromMissmatch?.Invoke();
                        }
                    }
                }
                else
                {
                    OnNotification("MATCHER", $"CANX IN:{matchCode} OUT:{CurrentUpdatetMatch}");
                }
            }
        }

        //TODO: what about on first load
        public void LocalBoardUpdate(string fen)
        {
            if (!string.IsNullOrWhiteSpace(fen) && LocalBoardFEN != fen)
            {
                LocalBoardFEN = fen;

                CurrentUpdatetMatch = Guid.NewGuid();
                Task.Run(() => TestForBoardMatch(CurrentUpdatetMatch.ToString()));

                OnLocalFenChange?.Invoke();
            }
        }

        public async void WatchStateChange(CherubApiMessage.MessageTypeCode messageType, BoardState remoteBoardState = null)
        {
            try
            {
                await startStopSemaphore.WaitAsync();

                if (messageType == MessageTypeCode.WATCH_STARTED)
                {
                    processUpdates = true;
                }
                else if (messageType == MessageTypeCode.WATCH_STOPPED)
                {
                    processUpdates = false;
                    ResetRemoteBoardState();
                }
            }
            finally { startStopSemaphore.Release(); }
        }

        public void RemoteBoardUpdated(BoardState remoteBoardState)
        {
            if (processUpdates && remoteBoardState.State.Code == ResponseCode.GAME_IN_PROGRESS)
            {
                fenProcessChannel.Writer.TryWrite(remoteBoardState);
                clockProcessChannel.Writer.TryWrite(remoteBoardState);
                lastMoveProcessChannel.Writer.TryWrite(remoteBoardState);
            }
            else
            {
                fenProcessChannel.Writer.TryWrite(remoteBoardState);
            }
        }

        public void UserMessageArrived(string source, string message)
        {
            messageProcessChannel.Writer.TryWrite((source, message));
        }

        private async void RunMessageProcessor()
        {
            (string source, string message) message;
            while ((message = await messageProcessChannel.Reader.ReadAsync()).source != null)
            {
                if (EchoExternalMessagesToConsole)
                {
                    OnNotification?.Invoke(message.source, message.message);
                }
            }
        }

        public async void RunClockProcessor()
        {
            for (; ; )
            {
                BoardState remoteBoardState = await clockProcessChannel.Reader.ReadAsync();
                UserMessageArrived("INGEST", $"Processing a clock recieved @ {remoteBoardState.CaptureTimeMs}");

                // Account for the actual time captured/now if clock running
                int captureTimeDiffMs = (int)((DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))).TotalMilliseconds - remoteBoardState.Board.Clocks.CaptureTimeMs);
                TimeSpan whiteTimespan = new(0, 0, 0, 0, remoteBoardState.Board.Clocks.WhiteClock - ((remoteBoardState.Board.Turn == TurnCode.WHITE) ? captureTimeDiffMs : 0));
                TimeSpan blackTimespan = new(0, 0, 0, 0, remoteBoardState.Board.Clocks.BlackClock - ((remoteBoardState.Board.Turn == TurnCode.BLACK) ? captureTimeDiffMs : 0));

                WhiteClockMs = (int)whiteTimespan.TotalMilliseconds;
                BlackClockMs = (int)blackTimespan.TotalMilliseconds;

                string whiteClockString = $"{whiteTimespan.Hours}:{whiteTimespan.Minutes.ToString().PadLeft(2, '0')}:{whiteTimespan.Seconds.ToString().PadLeft(2, '0')}";
                string blackClockString = $"{blackTimespan.Hours}:{blackTimespan.Minutes.ToString().PadLeft(2, '0')}:{blackTimespan.Seconds.ToString().PadLeft(2, '0')}";
                int runWho = remoteBoardState.Board.Turn == TurnCode.WHITE ? 1 : remoteBoardState.Board.Turn == TurnCode.BLACK ? 2 : 0;
                SetClocksStrings(whiteClockString, blackClockString, runWho.ToString());

                //Do this with the clock as it is the most responsive
                if (IsWhiteOnBottom != remoteBoardState.Board.IsWhiteOnBottom)
                {
                    IsWhiteOnBottom = remoteBoardState.Board.IsWhiteOnBottom;
                    OnOrientationFlipped?.Invoke();
                }
            }
        }

        public async void RunLastMoveProcessor()
        {
            for (; ; )
            {
                BoardState remoteBoardState = await lastMoveProcessChannel.Reader.ReadAsync();
                UserMessageArrived("INGEST", $"Processing a move  recieved @ {remoteBoardState.CaptureTimeMs}");

                if (LastMove == null || LastMove != remoteBoardState.Board.LastMove)
                {
                    LastMove = remoteBoardState.Board.LastMove;
                    OnNotification?.Invoke("LMOVE", $"New move detected '{remoteBoardState.Board.LastMove}'");
                    OnNewMoveDetected.Invoke(remoteBoardState.Board.LastMove);
                }
            }
        }

        public async void RunFenProcessor()
        {
            for (; ; )
            {
                BoardState remoteBoardState = await fenProcessChannel.Reader.ReadAsync();
                UserMessageArrived("INGEST", $"Processing a board recieved @ {remoteBoardState.CaptureTimeMs}");

                if (ChessDotComBoardFEN != remoteBoardState.Board.FenString)
                {
                    UserMessageArrived("INGEST", "FEN Change");

                    //_dgtEbDllFacade.SetClock(whiteClockString, blackClockString, runWho);
                    ChessDotComBoardFEN = remoteBoardState.Board.FenString;

                    CurrentUpdatetMatch = Guid.NewGuid();
                    _ = Task.Run(() => TestForBoardMatch(CurrentUpdatetMatch.ToString()));

                    OnRemoteFenChange?.Invoke();
                }
            }
        }

        public void ResetLocalBoardState()
        {
            LocalBoardFEN = "";
            IsMismatchDetected = false;
        }

        private void ResetRemoteBoardState()
        {
            ChessDotComBoardFEN = "";
            _chessDotComWhiteClock = "00:00";
            _chessDotComBlackClock = "00:00";
            _chessDotComRunWhoString = "0";
            OnClockChange?.Invoke();
            OnRemoteDisconnect?.Invoke();
            IsMismatchDetected = false;
            whiteNextClockAudioNotBefore = double.MaxValue;
            blackNextClockAudioNotBefore = double.MaxValue;
        }

        private void CalculateNextClockAudio(int clockMs, ref double nextAudioNotBefore, Action<string> onPlayAudio)
        {
            TimeSpan clockAudioTs;
            if ((clockAudioTs = TimeSpan.FromMilliseconds(clockMs)).TotalMilliseconds < nextAudioNotBefore )
            {
                bool isFirstCall = nextAudioNotBefore == double.MaxValue ? true : false;
                string audioFile = "";

                if (clockAudioTs.Hours > 0)
                {
                    nextAudioNotBefore = clockAudioTs.TotalMilliseconds - ((clockAudioTs.Hours * 3600000) + (clockAudioTs.Minutes * 60000) + (clockAudioTs.Seconds * 1000));
                }
                else if (clockAudioTs.Minutes > 20)
                {
                    nextAudioNotBefore = clockAudioTs.TotalMilliseconds - (((clockAudioTs.Minutes % 5) * 60000) + (clockAudioTs.Seconds * 1000));
                    audioFile = isFirstCall ? "" : $"M_{(clockAudioTs.Minutes + ((clockAudioTs.Seconds > 45 || clockAudioTs.Seconds < 15) ? 1 : 0)).ToString().PadLeft(2, '0')}";
                }
                else if (clockAudioTs.Minutes > 0)
                {
                    nextAudioNotBefore = clockAudioTs.TotalMilliseconds - (clockAudioTs.Seconds * 1000);
                    audioFile = isFirstCall ? "" : $"M_{(clockAudioTs.Minutes + ((clockAudioTs.Seconds > 45 || clockAudioTs.Seconds < 15) ? 1 : 0)).ToString().PadLeft(2, '0')}";
                }
                else if (clockAudioTs.Seconds > 30)
                {
                    nextAudioNotBefore = clockAudioTs.TotalMilliseconds - (5 * 1000);
                    audioFile = isFirstCall ? "" : $"S_{clockAudioTs.Seconds.ToString().PadLeft(2, '0')}";
                }
                else if (clockAudioTs.Seconds > 0)
                {
                    nextAudioNotBefore = clockAudioTs.TotalMilliseconds - (2 * 1000);
                    audioFile = isFirstCall ? "" :  $"S_{clockAudioTs.Seconds.ToString().PadLeft(2, '0')}";
                }

                // Play the audio if required
                if (!string.IsNullOrEmpty(audioFile)) { onPlayAudio?.Invoke(audioFile); }
            }
        }


        public void SetClocksStrings(string chessDotComWhiteClock, string chessDotComBlackClock, string chessDotComRunWhoString)
        {
            _chessDotComWhiteClock = chessDotComWhiteClock;
            _chessDotComBlackClock = chessDotComBlackClock;
            _chessDotComRunWhoString = chessDotComRunWhoString;
            OnClockChange?.Invoke();


            CalculateNextClockAudio(WhiteClockMs, ref whiteNextClockAudioNotBefore, (string audioFile) => OnPlayWhiteClockAudio?.Invoke(audioFile));
            CalculateNextClockAudio(BlackClockMs, ref blackNextClockAudioNotBefore, (string audioFile) => OnPlayBlackClockAudio?.Invoke(audioFile));
        }
    }
}




