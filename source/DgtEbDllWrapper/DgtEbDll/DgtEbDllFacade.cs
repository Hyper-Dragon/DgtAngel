using System;
using System.Runtime.CompilerServices;
using System.Text;
using static DgtRabbitWrapper.DgtEbDll.DgtEbDllAdapter;

namespace DgtRabbitWrapper.DgtEbDll
{
    public class DgtEbDllFacade : IDgtEbDllFacade
    {
        private string versionString = "";

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
        public void Init()
        {
            _ = DgtEbDllAdapter.Init();
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

