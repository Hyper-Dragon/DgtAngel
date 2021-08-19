using System;
using DgtAngelShared.Json;
using DgtEbDllWrapper;
using Microsoft.Extensions.Logging;

namespace DgtCherub.Services
{
    public interface IAppDataService
    {
        bool IsWhiteOnBottom { get; set; }
        bool IsMismatchDetected { get; set; }
        string BlackClock { get; }
        string ChessDotComBoardFEN { get; set; }
        bool EchoExternalMessagesToConsole { get; set; }
        bool PlayAudio { get; set; }
        bool IsChessDotComBoardStateActive { get; }
        string LocalBoardFEN { get; set; }
        string WhiteClock { get; }
        string RunWhoString { get; }

        event Action OnChessDotComFenChange;
        event Action OnChessDotComDisconnect;
        event Action OnClockChange;
        event Action OnLocalFenChange;
        event Action<string, string> OnUserMessageArrived;

        void RemoteBoardUpdated(BoardState remoteBoardState);
        void ResetChessDotComLocalBoardState();
        void ResetChessDotComRemoteBoardState();
        void SetClocks(string chessDotComWhiteClock, string chessDotComBlackClock, string chessDotComRunWhoString);
        void UserMessageArrived(string source, string message);
    }

    public sealed class AppDataService : IAppDataService
    {
        public event Action OnLocalFenChange;
        public event Action OnChessDotComFenChange;
        public event Action OnChessDotComDisconnect;
        public event Action OnClockChange;
        public event Action<string, string> OnUserMessageArrived;

        private string _localBoardFEN = "";
        private string _chessDotComFEN = "";
        private string _chessDotComWhiteClock = "00:00";
        private string _chessDotComBlackClock = "00:00";
        private string _chessDotComRunWhoString = "0";

        private readonly ILogger _logger;
        private readonly IDgtEbDllFacade _dgtEbDllFacade;

        public AppDataService(ILogger<AppDataService> logger, IDgtEbDllFacade dgtEbDllFacade)
        {
            _logger = logger;
            _dgtEbDllFacade = dgtEbDllFacade;
        }

        //public void LocalBoardUpdated(string fen)
        //{
        //
        //}

        public void RemoteBoardUpdated(BoardState remoteBoardState)
        {
            // Account for the actual time captured/now if clock running
            var captureTimeDiffMs = (int)((DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))).TotalMilliseconds - ((double)remoteBoardState.CaptureTimeMs));
            TimeSpan whiteTimespan = new(0, 0, 0, 0, remoteBoardState.Board.Clocks.WhiteClock - ((remoteBoardState.Board.Turn == TurnCode.WHITE) ? captureTimeDiffMs : 0));
            TimeSpan blackTimespan = new(0, 0, 0, 0, remoteBoardState.Board.Clocks.BlackClock - ((remoteBoardState.Board.Turn == TurnCode.BLACK) ? captureTimeDiffMs : 0));

            string whiteClockString = $"{whiteTimespan.Hours}:{whiteTimespan.Minutes.ToString().PadLeft(2, '0')}:{whiteTimespan.Seconds.ToString().PadLeft(2, '0')}";
            string blackClockString = $"{blackTimespan.Hours}:{blackTimespan.Minutes.ToString().PadLeft(2, '0')}:{blackTimespan.Seconds.ToString().PadLeft(2, '0')}";
            int runWho = remoteBoardState.Board.Turn == TurnCode.WHITE ? 1 : remoteBoardState.Board.Turn == TurnCode.BLACK ? 2 : 0;

            SetClocks(whiteClockString, blackClockString, runWho.ToString());
            //_dgtEbDllFacade.SetClock(whiteClockString, blackClockString, runWho);
            IsWhiteOnBottom = remoteBoardState.Board.IsWhiteOnBottom;
            ChessDotComBoardFEN = remoteBoardState.Board.FenString;
        }

        public bool IsWhiteOnBottom { get; set; } = true;
        public bool IsMismatchDetected { get; set; } = false;
        public bool EchoExternalMessagesToConsole { get; set; } = true;
        public string LocalBoardFEN
        {
            get => _localBoardFEN;
            set { if (string.IsNullOrEmpty(_localBoardFEN) || _localBoardFEN != value) { _localBoardFEN = value; OnLocalFenChange?.Invoke(); } }
        }
        public string ChessDotComBoardFEN
        {
            get => _chessDotComFEN;
            set
            {
                if (string.IsNullOrEmpty(_chessDotComFEN) || _chessDotComFEN != value)
                {

                        _chessDotComFEN = value;
                        OnChessDotComFenChange?.Invoke();

                }
            }
        }
        public string WhiteClock => _chessDotComWhiteClock;
        public string BlackClock => _chessDotComBlackClock;
        public string RunWhoString => _chessDotComRunWhoString;
        public bool IsChessDotComBoardStateActive => (_chessDotComFEN != "" && _chessDotComWhiteClock != "00:00" || _chessDotComBlackClock != "00:00");
        public bool PlayAudio { get; set; }

        public void ResetChessDotComLocalBoardState()
        {
            _localBoardFEN = "";
            IsMismatchDetected = false;
        }

        public void ResetChessDotComRemoteBoardState()
        {
            _chessDotComFEN = "";
            _chessDotComWhiteClock = "00:00";
            _chessDotComBlackClock = "00:00";
            _chessDotComRunWhoString = "0";
            OnChessDotComDisconnect?.Invoke();
            IsMismatchDetected = false;
        }

        public void SetClocks(string chessDotComWhiteClock, string chessDotComBlackClock, string chessDotComRunWhoString)
        {
            _chessDotComWhiteClock = chessDotComWhiteClock;
            _chessDotComBlackClock = chessDotComBlackClock;
            _chessDotComRunWhoString = chessDotComRunWhoString;
            OnClockChange?.Invoke();
        }

        //TODO: Track state transitions.... Send mismatch to clock option
        public void UserMessageArrived(string source, string message)
        {
            if (EchoExternalMessagesToConsole)
            {
                OnUserMessageArrived?.Invoke(source, message);
            }
        }
    }
}



