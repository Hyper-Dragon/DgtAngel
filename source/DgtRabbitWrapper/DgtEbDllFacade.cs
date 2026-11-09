using DgtRabbitWrapper.DgtEbDll;
using System;
using static DgtRabbitWrapper.DgtEbDll.DgtEbDllAdapter;

namespace DgtRabbitWrapper
{
    public class DgtEbDllFacade : IDgtEbDllFacade
    {
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

        private string versionString = "";


        public void NotifyOnStatusChanged(StatusMessageEventArgs message) { OnStatusMessage?.Invoke(null, message); }
        public void NotifyOnStableFenChanged(FenChangedEventArgs fenChange) { OnStableFenChanged?.Invoke(null, fenChange); }
        public void NotifyOnFenChanged(FenChangedEventArgs fenChange) { OnFenChanged?.Invoke(null, fenChange); }
        public void NotifyOnBClock(StatusMessageEventArgs data) { OnBClock?.Invoke(null, data); }
        public void NotifyOnBlackMoveInput(StatusMessageEventArgs data) { OnBlackMoveInput?.Invoke(null, data); }
        public void NotifyOnBlackMoveNow(StatusMessageEventArgs data) { OnBlackMoveNow?.Invoke(null, data); }
        public void NotifyOnNewGame(StatusMessageEventArgs data) { OnNewGame?.Invoke(null, data); }
        public void NotifyOnResult(StatusMessageEventArgs data) { OnResult?.Invoke(null, data); }
        public void NotifyOnStartSetup(StatusMessageEventArgs data) { OnStartSetup?.Invoke(null, data); }
        public void NotifyOnStopSetupBTM(StatusMessageEventArgs data) { OnStopSetupBTM?.Invoke(null, data); }
        public void NotifyOnStopSetupWTM(StatusMessageEventArgs data) { OnStopSetupWTM?.Invoke(null, data); }
        public void NotifyOnWClock(StatusMessageEventArgs data) { OnWClock?.Invoke(null, data); }
        public void NotifyOnWhiteMoveInput(StatusMessageEventArgs data) { OnWhiteMoveInput?.Invoke(null, data); }
        public void NotifyOnWhiteMoveNow(StatusMessageEventArgs data) { OnWhiteMoveNow?.Invoke(null, data); }



        public string GetRabbitVersionString()
        {
            return DgtDriverGate.Run(() =>
            {
                if (string.IsNullOrEmpty(versionString))
                {
                    versionString = GetVersion().versionTxt;
                }

                return versionString;
            });
        }

        public void ShowCongigDialog()
        {
            DgtDriverGate.Run(() => { _ = ShowDialog(); });
        }

        public void HideCongigDialog()
        {
            DgtDriverGate.Run(() => { _ = HideDialog(); });
        }

        public bool Init(IDgtEbDllFacade notifyTarget)
        {
            // The adapter takes the driver lock itself, around the native initialisation.
            return DgtEbDllAdapter.Init(notifyTarget);
        }

        public void DisplayMessage(string message, int time)
        {
            DgtDriverGate.Run(() =>
            {
                _ = DisplayClockMessage($"{message}", int.MaxValue);
                Thread.Sleep(time);
                _ = EndDisplay();
            });
        }

        public void DisplayForeverMessage(string message)
        {
            DgtDriverGate.Run(() => { _ = DisplayClockMessage($"{message}", int.MaxValue); });
        }

        public void StopForeverMessage()
        {
            DgtDriverGate.Run(() => { _ = EndDisplay(); });
        }

        public void DisplayMessageSeries(params string[] messages)
        {
            DgtDriverGate.Run(() => new Thread(new ThreadStart(() =>
            {
                _ = EndDisplay();
                Thread.Sleep(1000);

                foreach (string message in messages)
                {
                    _ = DisplayClockMessage(message, int.MaxValue);
                    Thread.Sleep(2000);
                }

                _ = EndDisplay();
            })).Start());
        }

        public void SetClock(string whiteClock, string blackClock, int runwho)
        {
            DgtDriverGate.Run(() => { _ = SetNRun($"{whiteClock}", $"{blackClock}", (RunWho)runwho); });
        }
    }
}