using System;

namespace DgtEbDllWrapper
{
    public sealed class FenChangedEventArgs : EventArgs
    {
        public long TimeChangedTicks { get; init; }
        public string Fen { get; init; }
    }

    public interface IDgtEbDllFacade
    {
        static event EventHandler<FenChangedEventArgs> OnFenChanged;

        void DisplayMessage(string message, int time);
        void DisplayForeverMessage(string message);
        void StopForeverMessage();
        void DisplayMessageSeries(params string[] messages);
        string GetRabbitVersionString();
        void HideCongigDialog();
        void Init();
        void SetClock(string whiteClock, string blackClock, int runwho);
        void ShowCongigDialog();
    }
}