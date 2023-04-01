﻿using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DgtRabbitWrapper.DgtEbDll
{
    public class DgtEbDllImport
    {
        /*
          Calls Not Wrapped 
          -----------------
          _DGTDLL_AllowTakebacks
          _DGTDLL_ChessBase_131e2d0711b4d50e
          _DGTDLL_Exit
          _DGTDLL_GetWxWidgetsVersion
          _DGTDLL_PlayBlackMove
          _DGTDLL_PlayWhiteMove
          _DGTDLL_SetAutoRotation
          _DGTDLL_SetGameType
          _DGTDLL_UseSAN
          _DGTDLL_WoodIn3_9c7b9ce70ec6a882
          _DGTDLL_WriteCOMPort
          _DGTDLL_WriteCOMPortString
          _DGTDLL_WriteDebug
          _DGTDLL_WritePosition

          Note
          ----
          typedef int __stdcall FC(const char*); 
          typedef int __stdcall FI(int); 
          typedef int __stdcall FB(bool); 
          typedef int __stdcall F(); 
          typedef int __stdcall FIIC(int, int, const char*);
        */

        private const string DGT_EB_DLL = @"dgtebdll.dll";

        public delegate void CallbackStatusFunc(string status);
        public delegate void CallbackStableBoardFunc(string boardFEN);
        public delegate void CallbackScanFunc(string boardFEN);

        [DllImport(DGT_EB_DLL, EntryPoint = "_DGTDLL_RegisterStatusFunc", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int RegisterStatusFunc(CallbackStatusFunc func, IntPtr callbackTarget);
        
        [DllImport(DGT_EB_DLL, EntryPoint = "_DGTDLL_RegisterStableBoardFunc", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int RegisterStableBoardFunc(CallbackStableBoardFunc func, IntPtr callbackTarget);
        
        [DllImport(DGT_EB_DLL, EntryPoint = "_DGTDLL_RegisterScanFunc", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int RegisterCallbackScanFunc(CallbackScanFunc func, IntPtr callbackTarget);

        //--------------------------------------------------------------------------------------

        public delegate void CallbackBClockFunc(string clock);
        public delegate void CallbackBlackMoveInputFunc(string move);
        public delegate void CallbackBlackMoveNowFunc(string move);
        public delegate void CallbackNewGameFunc(string game);
        public delegate void CallbackResultFunc(string result);
        public delegate void CallbackStartSetupFunc(string message);
        public delegate void CallbackStopSetupBTMFunc(string message);
        public delegate void CallbackStopSetupWTMFunc(string message);
        public delegate void CallbackWClockFunc(string clock);
        public delegate void CallbackWhiteMoveInputFunc(string move);
        public delegate void CallbackWhiteMoveNowFunc(string move);


        //public delegate void CallbackMagicPieceFunc(string VERIFY);
        //public delegate void CallbackAllowTakebacksChangedFunc(string VERIFY);
        //public delegate void CallbackGameTypeChangedFunc(string VERIFY);
        //public delegate void CallbackBlackTakebackFunc(string VERIFY);
        //public delegate void CallbackWhiteTakebackFunc(string VERIFY);

        [DllImport(DGT_EB_DLL, EntryPoint = "_DGTDLL_RegisterBClockFunc", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int RegisterBClockFunc(CallbackBClockFunc func, IntPtr callbackTarget);
        [DllImport(DGT_EB_DLL, EntryPoint = "_DGTDLL_RegisterBlackMoveInputFunc", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int RegisterBlackMoveInputFunc(CallbackBlackMoveInputFunc func, IntPtr callbackTarget);
        [DllImport(DGT_EB_DLL, EntryPoint = "_DGTDLL_RegisterBlackMoveNowFunc", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int RegisterBlackMoveNowFunc(CallbackBlackMoveNowFunc func, IntPtr callbackTarget);
        [DllImport(DGT_EB_DLL, EntryPoint = "_DGTDLL_RegisterNewGameFunc", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int RegisterNewGameFunc(CallbackNewGameFunc func, IntPtr callbackTarget);
        [DllImport(DGT_EB_DLL, EntryPoint = "_DGTDLL_RegisterResultFunc", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int RegisterResultFunc(CallbackResultFunc func, IntPtr callbackTarget);
        [DllImport(DGT_EB_DLL, EntryPoint = "_DGTDLL_RegisterStartSetupFunc", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int RegisterStartSetupFunc(CallbackStartSetupFunc func, IntPtr callbackTarget);
        [DllImport(DGT_EB_DLL, EntryPoint = "_DGTDLL_RegisterStopSetupBTMFunc", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int RegisterStopSetupBTMFunc(CallbackStopSetupBTMFunc func, IntPtr callbackTarget);
        [DllImport(DGT_EB_DLL, EntryPoint = "_DGTDLL_RegisterStopSetupWTMFunc", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int RegisterStopSetupWTMFunc(CallbackStopSetupWTMFunc func, IntPtr callbackTarget);
        [DllImport(DGT_EB_DLL, EntryPoint = "_DGTDLL_RegisterWClockFunc", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int RegisterWClockFunc(CallbackWClockFunc func, IntPtr callbackTarget);
        [DllImport(DGT_EB_DLL, EntryPoint = "_DGTDLL_RegisterWhiteMoveInputFunc", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int RegisterWhiteMoveInputFunc(CallbackWhiteMoveInputFunc func, IntPtr callbackTarget);
        [DllImport(DGT_EB_DLL, EntryPoint = "_DGTDLL_RegisterWhiteMoveNowFunc", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int RegisterWhiteMoveNowFunc(CallbackWhiteMoveNowFunc func, IntPtr callbackTarget);



        /*
                [DllImport(DGT_EB_DLL, EntryPoint = "_DGTDLL_RegisterMagicPieceFunc", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
                public static extern int RegisterMagicPieceFunc(CallbackMagicPieceFunc func, IntPtr callbackTarget);
                [DllImport(DGT_EB_DLL, EntryPoint = "_DGTDLL_RegisterAllowTakebacksChangedFunc", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
                public static extern int RegisterAllowTakebacksChangedFunc(CallbackAllowTakebacksChangedFunc func, IntPtr callbackTarget);
                [DllImport(DGT_EB_DLL, EntryPoint = "_DGTDLL_RegisterGameTypeChangedFunc", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
                public static extern int RegisterGameTypeChangedFunc(CallbackGameTypeChangedFunc func, IntPtr callbackTarget);
                [DllImport(DGT_EB_DLL, EntryPoint = "_DGTDLL_RegisterWhiteTakebackFunc", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
                public static extern int RegisterWhiteTakebackFunc(CallbackWhiteTakebackFunc func, IntPtr callbackTarget);
                [DllImport(DGT_EB_DLL, EntryPoint = "_DGTDLL_RegisterBlackTakebackFunc", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
                public static extern int RegisterBlackTakebackFunc(CallbackBlackTakebackFunc func, IntPtr callbackTarget);
        */
        //----------------------------------------------------------------------------------

        [DllImport(DGT_EB_DLL,
                   EntryPoint = "_DGTDLL_UseFEN",
                   ExactSpelling = true,
                   CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.StdCall)]
        public static extern int UseFEN(bool useFen);

        [DllImport(DGT_EB_DLL,
                   EntryPoint = "_DGTDLL_WriteDebug",
                   ExactSpelling = true,
                   CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.StdCall)]
        public static extern int WriteDebug(bool isDebugOn);


        //
        //int __stdcall (bool autorotate);
        //int __stdcall (bool value);
        //int __stdcall (int gameType);

        [DllImport(DGT_EB_DLL,
                   EntryPoint = "_DGTDLL_SetAutoRotation",
                   ExactSpelling = true,
                   CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.StdCall)]
        public static extern int SetAutoRotation(bool allowAutoRotate);

        [DllImport(DGT_EB_DLL,
           EntryPoint = "_DGTDLL_UseSAN",
           ExactSpelling = true,
           CharSet = CharSet.Ansi,
           CallingConvention = CallingConvention.StdCall)]
        public static extern int UseSAN(bool useSan);

        [DllImport(DGT_EB_DLL,
           EntryPoint = "_DGTDLL_SetGameType",
           ExactSpelling = true,
           CharSet = CharSet.Ansi,
           CallingConvention = CallingConvention.StdCall)]
        public static extern int SetGameType(int gameType);



        [DllImport(DGT_EB_DLL,
                   EntryPoint = "_DGTDLL_HideDialog",
                   ExactSpelling = true,
                   CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.StdCall)]
        public static extern int HideDialog(int dummy);

        [DllImport(DGT_EB_DLL,
                   EntryPoint = "_DGTDLL_ShowDialog",
                   ExactSpelling = true,
                   CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.StdCall)]
        public static extern int ShowDialog(int dummy);

        [DllImport(DGT_EB_DLL,
                   EntryPoint = "_DGTDLL_GetVersion",
                   ExactSpelling = true,
                   CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.StdCall)]
        public static extern int GetVersion();

        [DllImport(DGT_EB_DLL,
                   EntryPoint = "_DGTDLL_Init",
                   ExactSpelling = true,
                   CallingConvention = CallingConvention.StdCall)]
        public static extern int Init();

        [DllImport(DGT_EB_DLL,
                   EntryPoint = "_DGTDLL_ClockMode",
                   ExactSpelling = true,
                   CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.StdCall)]
        public static extern int ClockMode(int mode);

        [DllImport(DGT_EB_DLL,
                   EntryPoint = "_DGTDLL_SetNRun",
                   ExactSpelling = true,
                   CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.StdCall)]
        public static extern int SetNRun(StringBuilder wclock, StringBuilder bclock, int runwho);

        [DllImport(DGT_EB_DLL,
                   EntryPoint = "_DGTDLL_EndDisplay",
                   ExactSpelling = true,
                   CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.StdCall)]
        public static extern int EndDisplay(int dummy);

        // _DGTDLL_DisplayClockMessage(char* message, int time);
        [DllImport(DGT_EB_DLL,
                   EntryPoint = "_DGTDLL_DisplayClockMessage",
                   ExactSpelling = true,
                   CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.StdCall)]
        public static extern int DisplayClockMessage(StringBuilder message, int time);

    }
}
