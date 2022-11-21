using DgtRabbitWrapper.DgtEbDll;
using System;
using System.Runtime.CompilerServices;
using static DgtRabbitWrapper.DgtEbDll.DgtEbDllAdapter;

namespace DgtRabbitWrapper
{
    public class DgtEbDllFacade : IDgtEbDllFacade
    {
        public event EventHandler<FenChangedEventArgs> OnFenChanged;
        public event EventHandler<StatusMessageEventArgs> OnStatusMessage;

        private string versionString = "";


        public void NotifyOnStatusChanged(StatusMessageEventArgs message)
        {
            OnStatusMessage?.Invoke(null, message);
        }

        public void NotifyOnFenChanged(FenChangedEventArgs fenChange)
        {
            OnFenChanged?.Invoke(null, fenChange);
        }



        [MethodImpl(MethodImplOptions.Synchronized)]
        public string GetRabbitVersionString()
        {
            if (string.IsNullOrEmpty(versionString))
            {
                versionString = GetVersion().versionTxt;
            }

            return versionString;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ShowCongigDialog()
        {
            _ = ShowDialog();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void HideCongigDialog()
        {
            _ = HideDialog();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool Init(IDgtEbDllFacade notifyTarget)
        {
            return DgtEbDllAdapter.Init(notifyTarget);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void DisplayMessage(string message, int time)
        {
            _ = DisplayClockMessage($"{message}", int.MaxValue);
            Thread.Sleep(time);
            _ = EndDisplay();
        }

        public void DisplayForeverMessage(string message)
        {
            _ = DisplayClockMessage($"{message}", int.MaxValue);
        }

        public void StopForeverMessage()
        {
            _ = EndDisplay();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void DisplayMessageSeries(params string[] messages)
        {
            new Thread(new ThreadStart(() =>
            {
                _ = EndDisplay();
                Thread.Sleep(1000);

                foreach (string message in messages)
                {
                    _ = DisplayClockMessage(message, int.MaxValue);
                    Thread.Sleep(2000);
                }

                _ = EndDisplay();
            })).Start();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SetClock(string whiteClock, string blackClock, int runwho)
        {
            _ = SetNRun($"{whiteClock}", $"{blackClock}", (RunWho)runwho);
        }

    }
}