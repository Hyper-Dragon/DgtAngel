using System;
using System.Runtime.CompilerServices;
using System.Text;
using static DgtEbDllWrapper.DgtEbDllAdapter;

namespace DgtEbDllWrapper
{
    public class DgtEbDllFacade : IDgtEbDllFacade
    {
        private string versionString = "";

        [MethodImpl(MethodImplOptions.Synchronized)]
        public string GetRabbitVersionString()
        {
            if (string.IsNullOrEmpty(versionString))
            {
                versionString = DgtEbDllAdapter.GetVersion().versionTxt;
            }

            return versionString;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ShowCongigDialog()
        {
            _ = DgtEbDllAdapter.ShowDialog();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void HideCongigDialog()
        {
            _ = DgtEbDllAdapter.HideDialog();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Init()
        {
            _ = DgtEbDllAdapter.Init();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void DisplayMessage(string message, int time)
        {
            _ = DgtEbDllAdapter.DisplayClockMessage($"{message}", int.MaxValue);
            Thread.Sleep(time);
            _ = DgtEbDllAdapter.EndDisplay();
        }

        public void DisplayForeverMessage(string message)
        {
            _ = DgtEbDllAdapter.DisplayClockMessage($"{message}", int.MaxValue);
        }

        public void StopForeverMessage()
        {
            _ = DgtEbDllAdapter.EndDisplay();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void DisplayMessageSeries(params string[] messages)
        {
            new Thread(new ThreadStart(() =>
            {
                _ = DgtEbDllAdapter.EndDisplay();
                Thread.Sleep(1000);

                foreach (string message in messages)
                {
                    _ = DgtEbDllAdapter.DisplayClockMessage(message, int.MaxValue);
                    Thread.Sleep(2000);
                }

                _ = DgtEbDllAdapter.EndDisplay();
            })).Start();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SetClock(string whiteClock, string blackClock, int runwho)
        {
            _ = DgtEbDllAdapter.SetNRun($"{whiteClock}", $"{blackClock}", (RunWho)runwho);
        }



    }

}

