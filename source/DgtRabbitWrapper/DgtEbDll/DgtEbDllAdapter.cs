using System;
using System.Collections.Generic;
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

        // Added reference to prevent Garbage Collection on callbacks from the DLL
        // NOTE: public delegate void CallbackFunction([MarshalAs(UnmanagedType.LPStr)] String log); ?????
        private static readonly CallbackStatusFunc _callbackStatusInstance = new(CallbackStatusInstanceMethod);
        private static readonly CallbackStableBoardFunc _callbackStableBoardInstance = new(CallbackStableBoardInstanceMethod);
        private static readonly CallbackScanFunc _callbackSScanInstance = new(CallbackScanInstanceMethod);
        private static readonly CallbackBClockFunc _callbackBClockFunc = new(CallbackBClockFuncInstanceMethod);
        private static readonly CallbackBlackMoveInputFunc _callbackBlackMoveInputFunc = new(CallbackBlackMoveInputFuncInstanceMethod);
        private static readonly CallbackBlackMoveNowFunc _callbackBlackMoveNowFunc = new(CallbackBlackMoveNowFuncInstanceMethod);
        private static readonly CallbackNewGameFunc _callbackNewGameFunc = new(CallbackNewGameFuncInstanceMethod);
        private static readonly CallbackResultFunc _callbackResultFunc = new(CallbackResultFuncInstanceMethod);
        private static readonly CallbackStartSetupFunc _callbackStartSetupFunc = new(CallbackStartSetupFuncInstanceMethod);
        private static readonly CallbackStopSetupBTMFunc _callbackStopSetupBTMFunc = new(CallbackStopSetupBTMFuncInstanceMethod);
        private static readonly CallbackStopSetupWTMFunc _callbackStopSetupWTMFunc = new(CallbackStopSetupWTMFuncInstanceMethod);
        private static readonly CallbackWClockFunc _callbackWClockFunc = new(CallbackWClockFuncInstanceMethod);
        private static readonly CallbackWhiteMoveInputFunc _callbackWhiteMoveInputFunc = new(CallbackWhiteMoveInputFuncInstanceMethod);
        private static readonly CallbackWhiteMoveNowFunc _callbackWhiteMoveNowFunc = new(CallbackWhiteMoveNowFuncInstanceMethod);

        private static void CallbackStatusInstanceMethod(string message) { NotifyTarget.NotifyOnStatusChanged(new StatusMessageEventArgs() { Message = message, TimeChangedTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }); }

        private static void CallbackStableBoardInstanceMethod(string fenString) { NotifyTarget.NotifyOnStableFenChanged(new FenChangedEventArgs() { FEN = fenString, TimeChangedTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }); }

        private static void CallbackScanInstanceMethod(string fenString) { NotifyTarget.NotifyOnStableFenChanged(new FenChangedEventArgs() { FEN = fenString, TimeChangedTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }); }

        private static void CallbackBClockFuncInstanceMethod(string data) { NotifyTarget.NotifyOnBClock(new StatusMessageEventArgs() { Message = data, TimeChangedTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }); }

        private static void CallbackBlackMoveInputFuncInstanceMethod(string data) { NotifyTarget.NotifyOnBlackMoveInput(new StatusMessageEventArgs() { Message = data, TimeChangedTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }); }

        private static void CallbackBlackMoveNowFuncInstanceMethod(string data) { NotifyTarget.NotifyOnBlackMoveNow(new StatusMessageEventArgs() { Message = data, TimeChangedTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }); }

        private static void CallbackNewGameFuncInstanceMethod(string data) { NotifyTarget.NotifyOnNewGame(new StatusMessageEventArgs() { Message = data, TimeChangedTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }); }

        private static void CallbackResultFuncInstanceMethod(string data) { NotifyTarget.NotifyOnResult(new StatusMessageEventArgs() { Message = data, TimeChangedTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }); }

        private static void CallbackStartSetupFuncInstanceMethod(string data) { NotifyTarget.NotifyOnStartSetup(new StatusMessageEventArgs() { Message = data, TimeChangedTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }); }

        private static void CallbackStopSetupBTMFuncInstanceMethod(string data) { NotifyTarget.NotifyOnStopSetupBTM(new StatusMessageEventArgs() { Message = data, TimeChangedTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }); }

        private static void CallbackStopSetupWTMFuncInstanceMethod(string data) { NotifyTarget.NotifyOnStopSetupWTM(new StatusMessageEventArgs() { Message = data, TimeChangedTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }); }

        private static void CallbackWClockFuncInstanceMethod(string data) { NotifyTarget.NotifyOnWClock(new StatusMessageEventArgs() { Message = data, TimeChangedTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }); }

        private static void CallbackWhiteMoveInputFuncInstanceMethod(string data) { NotifyTarget.NotifyOnWhiteMoveInput(new StatusMessageEventArgs() { Message = data, TimeChangedTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }); }

        private static void CallbackWhiteMoveNowFuncInstanceMethod(string data) { NotifyTarget.NotifyOnWhiteMoveNow(new StatusMessageEventArgs() { Message = data, TimeChangedTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }); }


        internal static bool Init(IDgtEbDllFacade notifyTarget)
        {
            NotifyTarget = notifyTarget;

            Result result99 = (Result)DgtEbDllImport.GetVersion();

            Result result0 = (Result)DgtEbDllImport.Init();
            Result result1 = (Result)RegisterCallbackScanFunc(_callbackSScanInstance, IntPtr.Zero);
            Result result2 = (Result)RegisterStatusFunc(_callbackStatusInstance, IntPtr.Zero);
            Result result3 = (Result)SetGameType(0);
            Result result4 = (Result)UseSAN(true);
            Result result5 = (Result)SetAutoRotation(false);
            Result result6 = (Result)WriteDebug(false);
            Result result7 = (Result)UseFEN(true);
            _ = ConfigureAllCallbacks();

            return result0 == Result.SUCCESS && result1 == Result.SUCCESS && result2 == Result.SUCCESS &&
                   result3 == Result.SUCCESS && result4 == Result.SUCCESS && result5 == Result.SUCCESS &&
                   result6 == Result.SUCCESS && result7 == Result.SUCCESS && result7 == Result.SUCCESS;
        }

        private static Result ConfigureAllCallbacks()
        {
            List<Result> results = new()
            {
                (Result)RegisterStableBoardFunc(_callbackStableBoardInstance, IntPtr.Zero),
                (Result)RegisterBClockFunc(_callbackBClockFunc, IntPtr.Zero),
                (Result)RegisterBlackMoveInputFunc(_callbackBlackMoveInputFunc, IntPtr.Zero),
                (Result)RegisterBlackMoveNowFunc(_callbackBlackMoveNowFunc, IntPtr.Zero),
                (Result)RegisterNewGameFunc(_callbackNewGameFunc, IntPtr.Zero),
                (Result)RegisterResultFunc(_callbackResultFunc, IntPtr.Zero),
                (Result)RegisterStartSetupFunc(_callbackStartSetupFunc, IntPtr.Zero),
                (Result)RegisterStopSetupBTMFunc(_callbackStopSetupBTMFunc, IntPtr.Zero),
                (Result)RegisterStopSetupWTMFunc(_callbackStopSetupWTMFunc, IntPtr.Zero),
                (Result)RegisterWClockFunc(_callbackWClockFunc, IntPtr.Zero),
                (Result)RegisterWhiteMoveInputFunc(_callbackWhiteMoveInputFunc, IntPtr.Zero),
                (Result)RegisterWhiteMoveNowFunc(_callbackWhiteMoveNowFunc, IntPtr.Zero)
            };

            return results.Contains(Result.FAIL) ? Result.FAIL : Result.SUCCESS;
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
