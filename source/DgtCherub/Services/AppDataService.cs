using System;
using System.Threading.Tasks;
using DgtAngelShared.Json;
using DgtEbDllWrapper;
using Microsoft.Extensions.Logging;

namespace DgtCherub.Services
{
    public interface IAppDataService
    {
        string BlackClock { get; }
        string ChessDotComBoardFEN { get; }
        bool IsChessDotComBoardStateActive { get; }
        bool IsMismatchDetected { get; }
        bool IsWhiteOnBottom { get; }
        int WhiteClockMs { get; }
        int BlackClockMs { get; }
        string LastMove { get;  }
        string LocalBoardFEN { get; }
        string RunWhoString { get; }
        string WhiteClock { get; }

        event Action OnChessDotComDisconnect;
        event Action OnClockChange;
        event Action OnLocalFenChange;
        event Action OnRemoteFenChange;
        event Action OnBoardMissmatch;
        event Action OnBoardMatch;
        event Action<string, string> OnUserMessageArrived;

        void LocalBoardUpdate(string fen);
        void RemoteBoardUpdated(BoardState remoteBoardState);
        void ResetBoardState();
        void ResetRemoteBoardState();
        void SetClocksStrings(string chessDotComWhiteClock, string chessDotComBlackClock, string chessDotComRunWhoString);
        void UserMessageArrived(string source, string message);
    }

    public sealed class AppDataService : IAppDataService
    {
        public event Action OnLocalFenChange;
        public event Action OnRemoteFenChange;
        public event Action OnChessDotComDisconnect;
        public event Action OnClockChange;
        public event Action OnBoardMissmatch;
        public event Action OnBoardMatch;
        public event Action<string, string> OnUserMessageArrived;

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
        public int BlackClockMs  { get; private set; }
        public string WhiteClock => _chessDotComWhiteClock;
        public string BlackClock => _chessDotComBlackClock;
        public string RunWhoString => _chessDotComRunWhoString;
        public bool IsChessDotComBoardStateActive => (ChessDotComBoardFEN != "" && _chessDotComWhiteClock != "00:00" || _chessDotComBlackClock != "00:00");

        private readonly ILogger _logger;
        private readonly IDgtEbDllFacade _dgtEbDllFacade;

        public AppDataService(ILogger<AppDataService> logger, IDgtEbDllFacade dgtEbDllFacade)
        {
            _logger = logger;
            _dgtEbDllFacade = dgtEbDllFacade;
        }


        private async void TestForBoardMatch(string matchCode)
        {
            

            if (ChessDotComBoardFEN != LocalBoardFEN)
            {
                OnUserMessageArrived("MATCHER", $"MISS L:{LocalBoardFEN} Sync:{isBoardInSync}");
                OnUserMessageArrived("MATCHER", $"MISS R:{ChessDotComBoardFEN} Sync:{isBoardInSync}");
                isBoardInSync = false;
                OnBoardMissmatch?.Invoke();
            }
            else
            {
                OnUserMessageArrived("MATCHER", $"HIT  L:{LocalBoardFEN} Sync:{isBoardInSync}");
                OnUserMessageArrived("MATCHER", $"HIT  R:{ChessDotComBoardFEN} Sync:{isBoardInSync}");
                isBoardInSync = true;
                OnBoardMatch?.Invoke();
            }

       //
       //     //if (isBoardInSync)
       //     //{
       //         //await Task.Delay(2000);
       //
       //         //if (matchCode != currentUpdatetMatch.ToString())
       //         //{
       //             if (ChessDotComBoardFEN != LocalBoardFEN)
       //             {
       //                 isBoardInSync = false;
       //                 OnBoardMissmatch?.Invoke();
       //             }
       //         //}
       //     }
       //     else
       //     {
       //         //if (matchCode == currentUpdatetMatch.ToString())
       //         //{
       //         if (ChessDotComBoardFEN == LocalBoardFEN)
       //         {
       //             isBoardInSync = true;
       //             OnBoardMatch?.Invoke();
       //         }
       //         //}
       //     }
        }


        private Guid currentUpdatetMatch;
        private bool isBoardInSync = true;

        public void LocalBoardUpdate(string fen)
        {
            if (!string.IsNullOrWhiteSpace(fen) && LocalBoardFEN != fen)
            {
                LocalBoardFEN = fen;

                currentUpdatetMatch = Guid.NewGuid();
                Task.Run(() => TestForBoardMatch(currentUpdatetMatch.ToString()));

                OnLocalFenChange?.Invoke();
            }
        }

        public void RemoteBoardUpdated(BoardState remoteBoardState)
        {
            // Account for the actual time captured/now if clock running
            var captureTimeDiffMs = (int)((DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))).TotalMilliseconds - ((double)remoteBoardState.CaptureTimeMs));
            TimeSpan whiteTimespan = new(0, 0, 0, 0, remoteBoardState.Board.Clocks.WhiteClock - ((remoteBoardState.Board.Turn == TurnCode.WHITE) ? captureTimeDiffMs : 0));
            TimeSpan blackTimespan = new(0, 0, 0, 0, remoteBoardState.Board.Clocks.BlackClock - ((remoteBoardState.Board.Turn == TurnCode.BLACK) ? captureTimeDiffMs : 0));

            WhiteClockMs = (int) whiteTimespan.TotalMilliseconds;
            BlackClockMs = (int) blackTimespan.TotalMilliseconds;

            string whiteClockString = $"{whiteTimespan.Hours}:{whiteTimespan.Minutes.ToString().PadLeft(2, '0')}:{whiteTimespan.Seconds.ToString().PadLeft(2, '0')}";
            string blackClockString = $"{blackTimespan.Hours}:{blackTimespan.Minutes.ToString().PadLeft(2, '0')}:{blackTimespan.Seconds.ToString().PadLeft(2, '0')}";
            int runWho = remoteBoardState.Board.Turn == TurnCode.WHITE ? 1 : remoteBoardState.Board.Turn == TurnCode.BLACK ? 2 : 0;
            SetClocksStrings(whiteClockString, blackClockString, runWho.ToString());

            if (ChessDotComBoardFEN != remoteBoardState.Board.FenString)
            {
                //_dgtEbDllFacade.SetClock(whiteClockString, blackClockString, runWho);
                LastMove = remoteBoardState.Board.LastMove;
                IsWhiteOnBottom = remoteBoardState.Board.IsWhiteOnBottom;
                ChessDotComBoardFEN = remoteBoardState.Board.FenString;

                currentUpdatetMatch = Guid.NewGuid();
                Task.Run(() => TestForBoardMatch(currentUpdatetMatch.ToString()));

                OnRemoteFenChange?.Invoke();
            }
        }

        public void ResetBoardState()
        {
            LocalBoardFEN = "";
            IsMismatchDetected = false;
        }

        public void ResetRemoteBoardState()
        {
            ChessDotComBoardFEN = "";
            _chessDotComWhiteClock = "00:00";
            _chessDotComBlackClock = "00:00";
            _chessDotComRunWhoString = "0";
            OnClockChange?.Invoke();
            OnChessDotComDisconnect?.Invoke();
            IsMismatchDetected = false;
        }

        public void SetClocksStrings(string chessDotComWhiteClock, string chessDotComBlackClock, string chessDotComRunWhoString)
        {
            _chessDotComWhiteClock = chessDotComWhiteClock;
            _chessDotComBlackClock = chessDotComBlackClock;
            _chessDotComRunWhoString = chessDotComRunWhoString;
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



