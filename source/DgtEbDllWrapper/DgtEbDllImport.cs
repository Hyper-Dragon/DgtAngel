using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DgtEbDllWrapper
{
    internal class DgtEbDllImport
    {
        /*
        typedef int __stdcall FC(const char*); 
        typedef int __stdcall FI(int); 
        typedef int __stdcall FB(bool); 
        typedef int __stdcall F(); 
        typedef int __stdcall FIIC(int, int, const char*);
        */

        //---------------------------------------------------------------------

        internal delegate void CallbackScanFunc(StringBuilder boardFEN);

        [DllImport("dgtebdll.dll",
                   EntryPoint = "_DGTDLL_RegisterStableBoardFunc",
                   ExactSpelling = true,
                   CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.StdCall)]
        internal static extern int RegisterStableBoardFunc(CallbackScanFunc func, IntPtr callbackTarget);

        [DllImport("dgtebdll.dll",
                   EntryPoint = "_DGTDLL_RegisterScanFunc",
                   ExactSpelling = true,
                   CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.StdCall)]
        internal static extern int RegisterCallbackScanFunc(CallbackScanFunc func, IntPtr callbackTarget);


        [DllImport("dgtebdll.dll",
           EntryPoint = "_DGTDLL_UseFEN",
           ExactSpelling = true,
           CharSet = CharSet.Ansi,
           CallingConvention = CallingConvention.StdCall)]
        internal static extern int UseFEN(bool useFen);

        //---------------------------------------------------------------------

        [DllImport("dgtebdll.dll",
                   EntryPoint = "_DGTDLL_HideDialog",
                   ExactSpelling = true,
                   CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.StdCall)]
        internal static extern int HideDialog(int dummy);

        [DllImport("dgtebdll.dll",
                   EntryPoint = "_DGTDLL_ShowDialog",
                   ExactSpelling = true,
                   CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.StdCall)]
        internal static extern int ShowDialog(int dummy);

        [DllImport("dgtebdll.dll",
                   EntryPoint = "_DGTDLL_GetVersion",
                   ExactSpelling = true,
                   CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.StdCall)]
        internal static extern int GetVersion();

        [DllImport("dgtebdll.dll",
                   EntryPoint = "_DGTDLL_Init",
                   ExactSpelling = true,
                   CallingConvention = CallingConvention.StdCall)]
        internal static extern int Init();

        [DllImport("dgtebdll.dll",
                   EntryPoint = "_DGTDLL_ClockMode",
                   ExactSpelling = true,
                   CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.StdCall)]
        internal static extern int ClockMode(int mode);

        [DllImport("dgtebdll.dll",
                   EntryPoint = "_DGTDLL_SetNRun",
                   ExactSpelling = true,
                   CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.StdCall)]
        internal static extern int SetNRun(StringBuilder wclock, StringBuilder bclock, int runwho);

        [DllImport("dgtebdll.dll",
                   EntryPoint = "_DGTDLL_EndDisplay",
                   ExactSpelling = true,
                   CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.StdCall)]
        internal static extern int EndDisplay(int dummy);

        // _DGTDLL_DisplayClockMessage(char* message, int time);
        [DllImport("dgtebdll.dll",
                   EntryPoint = "_DGTDLL_DisplayClockMessage",
                   ExactSpelling = true,
                   CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.StdCall)]
        internal static extern int DisplayClockMessage(StringBuilder message, int time);


        /*
                 21    0 00001820 _DGTDLL_AllowTakebacks
                 42    1 00001F30 _DGTDLL_ChessBase_131e2d0711b4d50e
                 16    2 000015D0 _DGTDLL_ClockMode
                 13    3 00001440 _DGTDLL_DisplayClockMessage
                 14    4 000014C0 _DGTDLL_EndDisplay
                  4    5 000010A0 _DGTDLL_Exit
                  1    6 00001000 _DGTDLL_GetVersion
                  2    7 00001010 _DGTDLL_GetWxWidgetsVersion
                  6    8 00001140 _DGTDLL_HideDialog
                  3    9 00001020 _DGTDLL_Init
                 11    A 00001390 _DGTDLL_PlayBlackMove
                 10    B 00001310 _DGTDLL_PlayWhiteMove
                 39    C 00001E70 _DGTDLL_RegisterAllowTakebacksChangedFunc
                 26    D 00001A50 _DGTDLL_RegisterBClockFunc
                 30    E 00001B90 _DGTDLL_RegisterBlackMoveInputFunc
                 34    F 00001CD0 _DGTDLL_RegisterBlackMoveNowFunc
                 32   10 00001C30 _DGTDLL_RegisterBlackTakebackFunc
                 38   11 00001E10 _DGTDLL_RegisterGameTypeChangedFunc
                 40   12 00001ED0 _DGTDLL_RegisterMagicPieceFunc
                 28   13 00001AF0 _DGTDLL_RegisterNewGameFunc
                 27   14 00001AA0 _DGTDLL_RegisterResultFunc
                 23   15 00001900 _DGTDLL_RegisterScanFunc
                 24   16 00001950 _DGTDLL_RegisterStableBoardFunc
                 35   17 00001D20 _DGTDLL_RegisterStartSetupFunc
                 22   18 000018A0 _DGTDLL_RegisterStatusFunc
                 37   19 00001DC0 _DGTDLL_RegisterStopSetupBTMFunc
                 36   1A 00001D70 _DGTDLL_RegisterStopSetupWTMFunc
                 25   1B 00001A00 _DGTDLL_RegisterWClockFunc
                 29   1C 00001B40 _DGTDLL_RegisterWhiteMoveInputFunc
                 33   1D 00001C80 _DGTDLL_RegisterWhiteMoveNowFunc
                 31   1E 00001BE0 _DGTDLL_RegisterWhiteTakebackFunc
                 17   1F 00001620 _DGTDLL_SetAutoRotation
                 20   20 000017A0 _DGTDLL_SetGameType
                 15   21 00001540 _DGTDLL_SetNRun
                  5   22 00001120 _DGTDLL_ShowDialog
                 18   23 000016A0 _DGTDLL_UseFEN
                 19   24 00001720 _DGTDLL_UseSAN
                 41   25 00001F20 _DGTDLL_WoodIn3_9c7b9ce70ec6a882
                  7   26 00001160 _DGTDLL_WriteCOMPort
                  8   27 000011E0 _DGTDLL_WriteCOMPortString
                 12   28 00001410 _DGTDLL_WriteDebug
                  9   29 00001290 _DGTDLL_WritePosition


        */
    }
}
