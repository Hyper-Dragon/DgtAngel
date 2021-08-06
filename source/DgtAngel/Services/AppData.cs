using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DgtAngel.Services
{
    public interface IAppData
    {
        string BlackClock { get; }
        string BoardFEN { get; set; }
        string ChessDotComFEN { get; set; }
        bool IsChessDotComBoardStateActive { get; }
        string WhiteClock { get; }

        event Action OnClockChange;
        event Action OnFenChange;

        void ResetChessDotComBoardState();
        void SetClocks(string chessDotComWhiteClock, string chessDotComBlackClock);
    }

    public class AppData : IAppData
    {
        public event Action OnFenChange;
        public event Action OnClockChange;

        private string _boardFen = "";
        private string _chessDotComFEN = "";
        private string _chessDotComWhiteClock = "00:00";
        private string _chessDotComBlackClock = "00:00";

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
            if (chessDotComWhiteClock != _chessDotComWhiteClock || chessDotComBlackClock != _chessDotComBlackClock)
            {
                _chessDotComWhiteClock = chessDotComWhiteClock;
                _chessDotComBlackClock = chessDotComBlackClock;
                OnClockChange?.Invoke();
            }
        }
    }
}



