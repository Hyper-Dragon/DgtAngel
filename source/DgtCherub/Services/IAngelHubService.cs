﻿using DgtAngelShared.Json;
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
        void RemoteBoardUpdated(BoardState remoteBoardState);
        public void KillRemoteConnections();
        void ResetLocalBoardState();
        void UserMessageArrived(string source, string message);
        void WatchStateChange(MessageTypeCode messageType, string remoteSource, BoardState remoteBoardState = null);
        void PluginDisconnect();
    }
}
