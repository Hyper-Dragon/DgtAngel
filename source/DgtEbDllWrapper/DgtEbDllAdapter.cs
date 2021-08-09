using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DgtEbDllWrapper
{
    internal class DgtEbDllAdapter
    {
        internal enum Result { SUCCESS=0, FAIL };
        internal enum RunWho { PAUSE_BOTH = 0, RUN_WHITE, RUN_BLACK, RUN_BOTH };
        const int dummy = 0;

        /// <summary>
        /// Returns the version of the DLL.
        /// </summary>
        /// <returns>The version number of the Rabbit Plugin</returns>
        internal static (string major, string minor, string release, string versionTxt) GetVersion()
        {
            string resultStr = $"{DgtEbDllImport.GetVersion():000000}"; 

            return (resultStr.Substring(0, 2),
                    resultStr.Substring(2, 2),
                    resultStr.Substring(4, 2),
                    $"DGT Rabbit Version {resultStr.Substring(0, 2)}.{resultStr.Substring(2, 2)} rel. {resultStr.Substring(4, 2)}");
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

        internal static Result Init()
        {
            return (Result)DgtEbDllImport.Init();
        }

        /// <summary>
        /// Intends to check if the clock is in mode 23, but is not implemented in the board anyway.
        /// </summary>
        /// <returns>Translated from...23 if the clock is in mode 23, otherwise 0; But don’t rely on this result.</returns>
        internal static Result ClockMode()
        {
            return ((DgtEbDllImport.ClockMode(dummy)==23)?Result.SUCCESS:Result.FAIL);
        }

        internal static Result SetNRun(string whiteClock, string blackClock, RunWho runwho)
        {
            return (Result)DgtEbDllImport.SetNRun(new StringBuilder(whiteClock),new StringBuilder(blackClock), (int)runwho);
        }

    }
}
