using System;
using System.Text;
using static DgtRabbitWrapper.DgtEbDll.DgtEbDllImport;

namespace DgtRabbitWrapper.DgtEbDll
{
    internal class DgtEbDllAdapter
    {
        internal enum Result { SUCCESS = 0, FAIL };
        internal enum RunWho { PAUSE_BOTH = 0, RUN_WHITE, RUN_BLACK, RUN_BOTH };

        internal static IDgtEbDllFacade NotifyTarget;
        private const int dummy = 0;


        //public delegate void CallbackFunction([MarshalAs(UnmanagedType.LPStr)] String log);

        // Added reference to prevent Garbage Collection on callbacks from the DLL
        private static readonly CallbackStatusFunc _callbackStatusInstance = new(CallbackStatusInstanceMethod);
        private static readonly CallbackScanFunc _callbackStableBoardInstance = new(CallbackStableBoardInstanceMethod);


        static void CallbackStatusInstanceMethod(string message)
        {
            NotifyTarget.NotifyOnStatusChanged(new StatusMessageEventArgs(){
                                                   Message = message,
                                                   TimeChangedTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                                               });

        }

        static void CallbackStableBoardInstanceMethod(string fenString)
        {
            NotifyTarget.NotifyOnFenChanged(new FenChangedEventArgs(){
                                                    FEN = fenString,
                                                    TimeChangedTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                                                });                     
        }



        internal static bool Init(IDgtEbDllFacade notifyTarget)
        {
            NotifyTarget = notifyTarget;
            //int __stdcall _DGTDLL_SetGameType(int gameType);
            var result1 = (Result)DgtEbDllImport.Init();
            var result2 = (Result)RegisterStatusFunc(_callbackStatusInstance, IntPtr.Zero);
            var result3 = (Result)UseFEN(true);
            var result4 = (Result)RegisterStableBoardFunc(_callbackStableBoardInstance, IntPtr.Zero);

            return ((result1 == Result.SUCCESS && result2 == Result.SUCCESS && 
                     result3 == Result.SUCCESS && result4 == Result.SUCCESS) 
                     ? true : false);
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
            return DgtEbDllImport.ClockMode(dummy) == 23 ? Result.SUCCESS : Result.FAIL;
        }

        internal static Result SetNRun(string whiteClock, string blackClock, RunWho runwho)
        {
            return (Result)DgtEbDllImport.SetNRun(new StringBuilder(whiteClock), new StringBuilder(blackClock), (int)runwho);
        }

    }
}
