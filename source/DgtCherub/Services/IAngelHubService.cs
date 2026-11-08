using DgtAngelShared.Json;
using UciComms;
using UciComms.Data;
using static DgtAngelShared.Json.CherubApiMessage;

namespace DgtCherub.Services
{
    public interface IAngelHubService
    {
        bool IsClientInitComplete { get; }
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
        public int MatcherLocalDelayMs { get; set; }
        public int FromMismatchDelayMs { get; set; }

        event Action OnInitComplete;
        event Action<long, string> OnBoardMatch;
        event Action<string, bool> OnRemoteBoardStatusChange;
        event Action OnBoardMatcherStarted;
        event Action<long> OnBoardMatchFromMissmatch;
        event Action<long, int, string, string> OnBoardMissmatch;
        event Action OnRemoteDisconnect;
        event Action OnClockChange;
        event Action<string> OnLocalFenChange;
        event Action<string> OnRemoteWatchStarted;
        event Action<string> OnRemoteWatchStopped;
        event Action<string, bool> OnNewMoveDetected;
        event Action OnOrientationFlipped;
        event Action<string> OnPlayBlackClockAudio;
        event Action<string> OnPlayWhiteClockAudio;
        event Action<string, string, string, string, string, string, bool> OnRemoteFenChange;
        event Action<string, string> OnNotification;
        event Action OnPluginDisconnect;

        void NotifyInitComplete();
        void LocalBoardUpdate(string fen);

        /// <summary>
        /// When true a connected desktop client (through the DGTEBDLL proxy) owns the DGT
        /// clock: its clock calls are passed through and Cherub stops writing times itself,
        /// so the two cannot fight over the same display.  When false Cherub owns the clock
        /// and clock calls arriving over gRPC are acknowledged but not forwarded.
        /// </summary>
        bool DesktopClientMode { get; set; }
        void RemoteBoardUpdated(BoardState remoteBoardState);
        void ResetLocalBoardState();
        void UserMessageArrived(string source, string message);
        Task WatchStateChange(MessageTypeCode messageType, string remoteSource, BoardState remoteBoardState = null);
        void PluginDisconnect();
    }
}
