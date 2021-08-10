using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DgtCherub
{
    public interface IAppDataService
    {
        bool EchoExternalMessagesToConsole { get; set; }
        string BlackClock { get; }
        string BoardFEN { get; set; }
        string ChessDotComFEN { get; set; }
        bool IsChessDotComBoardStateActive { get; }
        string WhiteClock { get; }

        event Action OnClockChange;
        event Action OnFenChange;
        event Action<string, string> OnUserMessageArrived;

        void ResetChessDotComBoardState();
        void SetClocks(string chessDotComWhiteClock, string chessDotComBlackClock);
        void UserMessageArrived(string source, string message);
    }

    public class AppDataService : IAppDataService
    {
        public event Action OnFenChange;
        public event Action OnClockChange;
        public event Action<string, string> OnUserMessageArrived;

        private string _boardFen = "";
        private string _chessDotComFEN = "";
        private string _chessDotComWhiteClock = "00:00";
        private string _chessDotComBlackClock = "00:00";

        public bool EchoExternalMessagesToConsole { get; set; } = true;
        public string BoardFEN { get { return _boardFen; } set { if (_boardFen != value) { _boardFen = value; OnFenChange?.Invoke(); } } }
        public string ChessDotComFEN { get { return _chessDotComFEN; } set { if (_chessDotComFEN != value) { _chessDotComFEN = value; OnFenChange?.Invoke(); } } }
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



