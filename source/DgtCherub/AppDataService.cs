using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DgtCherub
{
    public interface IAppDataService
    {
        string BlackClock { get; }
        string ChessDotComBoardFEN { get; set; }
        bool EchoExternalMessagesToConsole { get; set; }
        bool IsChessDotComBoardStateActive { get; }
        string LocalBoardFEN { get; set; }
        string WhiteClock { get; }

        event Action OnChessDotComFenChange;
        event Action OnClockChange;
        event Action OnLocalFenChange;
        event Action<string, string> OnUserMessageArrived;

        void ResetChessDotComBoardState();
        void SetClocks(string chessDotComWhiteClock, string chessDotComBlackClock);
        void UserMessageArrived(string source, string message);
    }

    public class AppDataService : IAppDataService
    {
        public event Action OnLocalFenChange;
        public event Action OnChessDotComFenChange;
        public event Action OnClockChange;
        public event Action<string, string> OnUserMessageArrived;

        private string _localBoardFEN = "8/8/8/8/8/8/8/8";
        private string _chessDotComFEN = "8/8/8/8/8/8/8/8";
        private string _chessDotComWhiteClock = "00:00";
        private string _chessDotComBlackClock = "00:00";

        public bool EchoExternalMessagesToConsole { get; set; } = true;
        public string LocalBoardFEN { get { return _localBoardFEN; } set { if (_localBoardFEN != value) { _localBoardFEN = value; OnLocalFenChange?.Invoke(); } } }
        public string ChessDotComBoardFEN { get { return _chessDotComFEN; } set { if (_chessDotComFEN != value) { _chessDotComFEN = value; OnChessDotComFenChange?.Invoke(); } } }
        public string WhiteClock { get { return _chessDotComWhiteClock; } }
        public string BlackClock { get { return _chessDotComBlackClock; } }
        public bool IsChessDotComBoardStateActive { get { return (_chessDotComFEN != "" && _chessDotComWhiteClock != "00:00" || _chessDotComBlackClock != "00:00"); } }

        public void ResetChessDotComBoardState()
        {
            _chessDotComFEN = "";
            _chessDotComWhiteClock = "00:00";
            _chessDotComBlackClock = "00:00";
        }

        public void SetClocks(string chessDotComWhiteClock, string chessDotComBlackClock)
        {

            _chessDotComWhiteClock = chessDotComWhiteClock;
            _chessDotComBlackClock = chessDotComBlackClock;
            OnClockChange?.Invoke();

        }

        public void UserMessageArrived(string source, string message)
        {
            if (EchoExternalMessagesToConsole)
            {
                OnUserMessageArrived?.Invoke(source, message);
            }
        }
    }
}



