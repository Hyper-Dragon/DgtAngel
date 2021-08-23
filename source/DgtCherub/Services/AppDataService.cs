using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DgtAngelShared.Json;
using DgtEbDllWrapper;
using Microsoft.Extensions.Logging;

namespace DgtCherub.Services
{
    public interface IAppDataService
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
        event Action OnBoardMatchFromMissmatch;
        event Action OnBoardMissmatch;
        event Action OnBoardMatcherStarted;
        event Action OnChessDotComDisconnect;
        event Action OnClockChange;
        event Action OnLocalFenChange;
        event Action OnOrientationFlipped;
        event Action OnRemoteFenChange;
        event Action<string> OnNewMoveDetected;
        event Action<string> OnPlayWhiteClockAudio;
        event Action<string> OnPlayBlackClockAudio;
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
        const int MATCHER_TIME_DELAY_MS = 3000;

        public event Action OnLocalFenChange;
        public event Action OnRemoteFenChange;
        public event Action OnChessDotComDisconnect;
        public event Action OnClockChange;
        public event Action OnOrientationFlipped;
        public event Action OnBoardMissmatch;
        public event Action OnBoardMatcherStarted;
        public event Action OnBoardMatch;
        public event Action OnBoardMatchFromMissmatch;
        public event Action<string> OnNewMoveDetected;
        public event Action<string> OnPlayWhiteClockAudio;
        public event Action<string> OnPlayBlackClockAudio;
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
        public int BlackClockMs { get; private set; }
        public string WhiteClock => _chessDotComWhiteClock;
        public string BlackClock => _chessDotComBlackClock;
        private int BlackNextClockAudioNotBefore { get; set; } = int.MaxValue;
        private int WhiteNextClockAudioNotBefore { get; set; } = int.MaxValue;
        public string RunWhoString => _chessDotComRunWhoString;
        public bool IsLocalBoardAvailable => (!string.IsNullOrWhiteSpace(LocalBoardFEN));
        public bool IsRemoteBoardAvailable => (!string.IsNullOrWhiteSpace(ChessDotComBoardFEN));
        public bool IsBoardInSync { get; private set; } = true;
        public bool IsChessDotComBoardStateActive => (ChessDotComBoardFEN != "" && _chessDotComWhiteClock != "00:00" || _chessDotComBlackClock != "00:00");
        private Guid CurrentUpdatetMatch { get; set; } = Guid.NewGuid();

        //private readonly ConcurrentQueue<string> moveQueue=new();


        private readonly ILogger _logger;
        private readonly IDgtEbDllFacade _dgtEbDllFacade;
        
        private readonly static object fenChangeLock=new();

        public AppDataService(ILogger<AppDataService> logger, IDgtEbDllFacade dgtEbDllFacade)
        {
            _logger = logger;
            _dgtEbDllFacade = dgtEbDllFacade;
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
                    //OnUserMessageArrived("MATCHER", $"POST IN:{matchCode} OUT:{CurrentUpdatetMatch}");

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
                    //OnUserMessageArrived("MATCHER", $"CANX IN:{matchCode} OUT:{CurrentUpdatetMatch}");
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

        public void RemoteBoardUpdated(BoardState remoteBoardState)
        {
            // Account for the actual time captured/now if clock running
            var captureTimeDiffMs = (int)((DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))).TotalMilliseconds - ((double)remoteBoardState.CaptureTimeMs));
            TimeSpan whiteTimespan = new(0, 0, 0, 0, remoteBoardState.Board.Clocks.WhiteClock - ((remoteBoardState.Board.Turn == TurnCode.WHITE) ? captureTimeDiffMs : 0));
            TimeSpan blackTimespan = new(0, 0, 0, 0, remoteBoardState.Board.Clocks.BlackClock - ((remoteBoardState.Board.Turn == TurnCode.BLACK) ? captureTimeDiffMs : 0));

            WhiteClockMs = (int)whiteTimespan.TotalMilliseconds;
            BlackClockMs = (int)blackTimespan.TotalMilliseconds;

            string whiteClockString = $"{whiteTimespan.Hours}:{whiteTimespan.Minutes.ToString().PadLeft(2, '0')}:{whiteTimespan.Seconds.ToString().PadLeft(2, '0')}";
            string blackClockString = $"{blackTimespan.Hours}:{blackTimespan.Minutes.ToString().PadLeft(2, '0')}:{blackTimespan.Seconds.ToString().PadLeft(2, '0')}";
            int runWho = remoteBoardState.Board.Turn == TurnCode.WHITE ? 1 : remoteBoardState.Board.Turn == TurnCode.BLACK ? 2 : 0;
            SetClocksStrings(whiteClockString, blackClockString, runWho.ToString());

            if (IsWhiteOnBottom != remoteBoardState.Board.IsWhiteOnBottom)
            {
                IsWhiteOnBottom = remoteBoardState.Board.IsWhiteOnBottom;
                OnOrientationFlipped?.Invoke();
            }

            try
            {
                Monitor.Enter(fenChangeLock);
                if (ChessDotComBoardFEN != remoteBoardState.Board.FenString)
                {
                    //_dgtEbDllFacade.SetClock(whiteClockString, blackClockString, runWho);
                    LastMove = remoteBoardState.Board.LastMove;
                    ChessDotComBoardFEN = remoteBoardState.Board.FenString;

                    CurrentUpdatetMatch = Guid.NewGuid();
                    Task.Run(() => TestForBoardMatch(CurrentUpdatetMatch.ToString()));

                    OnUserMessageArrived?.Invoke("LMOVE", remoteBoardState.Board.LastMove);
                    OnNewMoveDetected?.Invoke(remoteBoardState.Board.LastMove);
                    OnRemoteFenChange?.Invoke();
                }
            }
            catch { throw;  }
            finally
            {
                Monitor.Exit(fenChangeLock);
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
            WhiteNextClockAudioNotBefore = int.MaxValue;
            BlackNextClockAudioNotBefore = int.MaxValue;
        }

        public void SetClocksStrings(string chessDotComWhiteClock, string chessDotComBlackClock, string chessDotComRunWhoString)
        {
            _chessDotComWhiteClock = chessDotComWhiteClock;
            _chessDotComBlackClock = chessDotComBlackClock;
            _chessDotComRunWhoString = chessDotComRunWhoString;
            OnClockChange?.Invoke();

            TimeSpan clockAudioWhiteTs;
            if ((clockAudioWhiteTs = TimeSpan.FromMilliseconds(WhiteClockMs)).TotalMilliseconds <= WhiteNextClockAudioNotBefore)
            {
                if (clockAudioWhiteTs.Hours < 1)
                {
                    if (clockAudioWhiteTs.Minutes > 20)
                    {
                        WhiteNextClockAudioNotBefore = (int)clockAudioWhiteTs.TotalMilliseconds - (((clockAudioWhiteTs.Minutes % 5) * 60000) + ((60 - clockAudioWhiteTs.Seconds) * 1000));
                        OnPlayWhiteClockAudio?.Invoke($"M_{clockAudioWhiteTs.Minutes.ToString().PadLeft(2, '0')}");
                    }
                    else if (clockAudioWhiteTs.Minutes > 0)
                    {
                        WhiteNextClockAudioNotBefore = (int)clockAudioWhiteTs.TotalMilliseconds - ((60 - clockAudioWhiteTs.Seconds) * 1000);
                        OnPlayWhiteClockAudio?.Invoke($"M_{clockAudioWhiteTs.Minutes.ToString().PadLeft(2, '0')}");
                    }
                    else if (clockAudioWhiteTs.Seconds > 30)
                    {
                        WhiteNextClockAudioNotBefore = (int)clockAudioWhiteTs.TotalMilliseconds - (5 * 1000);
                        OnPlayWhiteClockAudio?.Invoke($"S_{clockAudioWhiteTs.Seconds.ToString().PadLeft(2, '0')}");
                    }
                    else if (clockAudioWhiteTs.Seconds > 0)
                    {
                        WhiteNextClockAudioNotBefore = (int)clockAudioWhiteTs.TotalMilliseconds - (2 * 1000);
                        OnPlayWhiteClockAudio?.Invoke($"S_{clockAudioWhiteTs.Seconds.ToString().PadLeft(2, '0')}");
                    }
                }
            }

            TimeSpan clockAudioBlackTs;
            if ((clockAudioBlackTs = TimeSpan.FromMilliseconds(BlackClockMs)).TotalMilliseconds <= BlackNextClockAudioNotBefore)
            {
                if (clockAudioBlackTs.Hours < 1)
                {
                    if (clockAudioBlackTs.Minutes > 20)
                    {
                        BlackNextClockAudioNotBefore = (int)clockAudioBlackTs.TotalMilliseconds - (((clockAudioWhiteTs.Minutes % 5) * 60000) + ((60 - clockAudioWhiteTs.Seconds) * 1000));
                        OnPlayBlackClockAudio?.Invoke($"M_{clockAudioBlackTs.Minutes.ToString().PadLeft(2, '0')}");
                    }
                    else if (clockAudioBlackTs.Minutes > 0)
                    {
                        BlackNextClockAudioNotBefore = (int)clockAudioBlackTs.TotalMilliseconds - ((60 - clockAudioBlackTs.Seconds) * 1000);
                        OnPlayBlackClockAudio?.Invoke($"M_{clockAudioBlackTs.Minutes.ToString().PadLeft(2, '0')}");
                    }
                    else if (clockAudioBlackTs.Seconds > 30)
                    {
                        BlackNextClockAudioNotBefore = (int)clockAudioBlackTs.TotalMilliseconds - (5 * 1000);
                        OnPlayBlackClockAudio?.Invoke($"S_{clockAudioBlackTs.Seconds.ToString().PadLeft(2, '0')}");
                    }
                    else if (clockAudioBlackTs.Seconds > 0)
                    {
                        BlackNextClockAudioNotBefore = (int)clockAudioBlackTs.TotalMilliseconds - (2 * 1000);
                        OnPlayBlackClockAudio?.Invoke($"S_{clockAudioBlackTs.Seconds.ToString().PadLeft(2, '0')}");
                    }
                }
            }
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



