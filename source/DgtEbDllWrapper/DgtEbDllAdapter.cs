using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static DgtEbDllWrapper.DgtEbDllAdapter;
using static DgtEbDllWrapper.DgtEbDllImport;

namespace DgtEbDllWrapper
{
    internal class DgtEbDllAdapter
    {
        internal enum Result { SUCCESS = 0, FAIL };
        internal enum RunWho { PAUSE_BOTH = 0, RUN_WHITE, RUN_BLACK, RUN_BOTH };

        private const int dummy = 0;

        // Note: this method will be called from a different thread!
        public static event EventHandler<FenChangedEventArgs> OnFenChanged;

        
        //public delegate void CallbackFunction([MarshalAs(UnmanagedType.LPStr)] String log);

        // add static reference....
        private static readonly CallbackScanFunc _callbackInstance = new(MethodA); // Added reference to prevent Garbage Collection 



        static void MethodA(String message)
        {
            OnFenChanged?.Invoke( null, new FenChangedEventArgs() { Fen=message, TimeChangedTicks=DateTime.UtcNow.Ticks} );
            Console.WriteLine("hello");
        }

        internal static Result Init()
        {
            return (Result)Init(_callbackInstance);
        }

        internal static Result Init(CallbackScanFunc func)
        {
            var result1 = (Result)DgtEbDllImport.Init();
            var result2 = (Result)DgtEbDllImport.UseFEN(true);
            var result3 = (Result)DgtEbDllImport.RegisterStableBoardFunc(_callbackInstance, IntPtr.Zero);

            return (result1 == Result.SUCCESS && result2 == Result.SUCCESS && result3 == Result.SUCCESS) ? Result.SUCCESS : Result.FAIL;
        }


        /// <summary>
        /// Returns the version of the DLL.
        /// </summary>
        /// <returns>The version number of the Rabbit Plugin</returns>
        internal static (string major, string minor, string release, string versionTxt) GetVersion()
        {
            string resultStr = $"{DgtEbDllImport.GetVersion():000000}";

            return (resultStr[..2],
                    resultStr.Substring(2, 2),
                    resultStr.Substring(4, 2),
                    $"DGT Rabbit Version {resultStr[..2]}.{resultStr.Substring(2, 2)} rel. {resultStr.Substring(4, 2)}");
        }

        internal static Result ShowDialog()
        {
            return (Result)DgtEbDllImport.ShowDialog(dummy);
        }

        internal static Result HideDialog()
        {
            return (Result)DgtEbDllImport.HideDialog(dummy);
        }

        internal static Result DisplayClockMessage(string message, int time)
        {
            return (Result)DgtEbDllImport.DisplayClockMessage(new StringBuilder($"{message}"), time);
        }

        internal static Result EndDisplay()
        {
            return (Result)DgtEbDllImport.EndDisplay(dummy);
        }


        /// <summary>
        /// Intends to check if the clock is in mode 23, but is not implemented in the board anyway.
        /// </summary>
        /// <returns>Translated from...23 if the clock is in mode 23, otherwise 0; But don’t rely on this result.</returns>
        internal static Result ClockMode()
        {
            return (DgtEbDllImport.ClockMode(dummy) == 23) ? Result.SUCCESS : Result.FAIL;
        }

        internal static Result SetNRun(string whiteClock, string blackClock, RunWho runwho)
        {
            return (Result)DgtEbDllImport.SetNRun(new StringBuilder(whiteClock), new StringBuilder(blackClock), (int)runwho);
        }

    }
}
