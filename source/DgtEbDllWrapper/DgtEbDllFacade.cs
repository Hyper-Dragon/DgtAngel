using System.Runtime.CompilerServices;
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
            DgtEbDllAdapter.ShowDialog();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void HideCongigDialog()
        {
            DgtEbDllAdapter.HideDialog();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Init()
        {
            DgtEbDllAdapter.Init();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void DisplayMessage(string message, int time)
        {
            DgtEbDllAdapter.DisplayClockMessage($"{message}", int.MaxValue);
            Thread.Sleep(time);
            DgtEbDllAdapter.EndDisplay();
        }

        public void DisplayForeverMessage(string message)
        {
            DgtEbDllAdapter.DisplayClockMessage($"{message}", int.MaxValue);
        }

        public void StopForeverMessage()
        {
            DgtEbDllAdapter.EndDisplay();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void DisplayMessageSeries(params string[] messages)
        {
            new Thread(new ThreadStart(() =>
            {
                DgtEbDllAdapter.EndDisplay();
                Thread.Sleep(1000);

                foreach (string message in messages)
                {
                    DgtEbDllAdapter.DisplayClockMessage(message, int.MaxValue);
                    Thread.Sleep(2000);
                }

                DgtEbDllAdapter.EndDisplay();
            })).Start();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SetClock(string whiteClock, string blackClock, int runwho)
        {
            DgtEbDllAdapter.SetNRun($"{whiteClock}", $"{blackClock}", (RunWho)runwho);
        }



    }

}

