using System;

namespace DgtRabbitWrapper.DgtEbDll
{
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
        public event EventHandler<StatusMessageEventArgs> OnStatusMessage;


        void NotifyOnStatusChanged(StatusMessageEventArgs message);
        void NotifyOnFenChanged(FenChangedEventArgs fenChange);

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