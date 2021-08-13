﻿using System;

namespace DgtCherub
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

        void ResetChessDotComLocalBoardState();
        void ResetChessDotComRemoteBoardState();
        void SetClocks(string chessDotComWhiteClock, string chessDotComBlackClock, string chessDotComRunWhoString);
        void UserMessageArrived(string source, string message);
    }

    public class AppDataService : IAppDataService
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



