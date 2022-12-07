using System;

namespace DgtRabbitWrapper.DgtEbDll
{
    public sealed class RabbitEventArgs : EventArgs
    {
        public long TimeChangedTicks { get; init; }
        public string Message { get; init; }
    }

    public sealed class FenChangedEventArgs : EventArgs
    {
        public long TimeChangedTicks { get; init; }
        public string FEN { get; init; }
    }

    public sealed class StatusMessageEventArgs : EventArgs
    {
        public long TimeChangedTicks { get; init; }
        public string Message { get; init; }
    }

    public interface IDgtEbDllFacade
    {
        // Note: this method will be called from a different thread!
        public event EventHandler<FenChangedEventArgs> OnFenChanged;
        public event EventHandler<FenChangedEventArgs> OnStableFenChanged;
        public event EventHandler<StatusMessageEventArgs> OnStatusMessage;
        public event EventHandler<StatusMessageEventArgs> OnBClock;
        public event EventHandler<StatusMessageEventArgs> OnBlackMoveInput;
        public event EventHandler<StatusMessageEventArgs> OnBlackMoveNow;
        public event EventHandler<StatusMessageEventArgs> OnNewGame;
        public event EventHandler<StatusMessageEventArgs> OnResult;
        public event EventHandler<StatusMessageEventArgs> OnStartSetup;
        public event EventHandler<StatusMessageEventArgs> OnStopSetupBTM;
        public event EventHandler<StatusMessageEventArgs> OnStopSetupWTM;
        public event EventHandler<StatusMessageEventArgs> OnWClock;
        public event EventHandler<StatusMessageEventArgs> OnWhiteMoveInput;
        public event EventHandler<StatusMessageEventArgs> OnWhiteMoveNow;

        void NotifyOnStatusChanged(StatusMessageEventArgs message);
        void NotifyOnStableFenChanged(FenChangedEventArgs fenChange);
        void NotifyOnFenChanged(FenChangedEventArgs fenChange);
        void NotifyOnBClock(StatusMessageEventArgs data);
        void NotifyOnBlackMoveInput(StatusMessageEventArgs data);
        void NotifyOnBlackMoveNow(StatusMessageEventArgs data);
        void NotifyOnNewGame(StatusMessageEventArgs data);
        void NotifyOnResult(StatusMessageEventArgs data);
        void NotifyOnStartSetup(StatusMessageEventArgs data);
        void NotifyOnStopSetupBTM(StatusMessageEventArgs data);
        void NotifyOnStopSetupWTM(StatusMessageEventArgs data);
        void NotifyOnWClock(StatusMessageEventArgs data);
        void NotifyOnWhiteMoveInput(StatusMessageEventArgs data);
        void NotifyOnWhiteMoveNow(StatusMessageEventArgs data);




        void DisplayMessage(string message, int time);
        void DisplayForeverMessage(string message);
        void StopForeverMessage();
        void DisplayMessageSeries(params string[] messages);
        string GetRabbitVersionString();
        void HideCongigDialog();
        bool Init(IDgtEbDllFacade notifyTarget);
        void SetClock(string whiteClock, string blackClock, int runwho);
        void ShowCongigDialog();
    }
}