using DgtRabbitWrapper.DgtEbDll;

namespace DgtCherub.Services
{
    /// <summary>
    /// Owns the one and only registration of the native dgtebdll.dll callbacks.
    /// </summary>
    /// <remarks>
    /// The gRPC service is instantiated per call, so registering the callbacks from
    /// its constructor re-registered every delegate once per request: the previous
    /// instances' delegates were never released and every board event was then
    /// raised once for each accumulated registration.  This class is registered as
    /// a singleton and keeps every delegate in a field, which also roots them - the
    /// native DLL only holds raw function pointers, so a collected delegate would
    /// crash the process on the next board event.
    /// </remarks>
    public interface IBoardEventSource
    {
        event EventHandler<string> StableBoard;
        event EventHandler<string> Status;
        event EventHandler<string> Scan;
        event EventHandler<string> WClock;
        event EventHandler<string> BClock;
        event EventHandler<string> Result;
        event EventHandler<string> NewGame;
        event EventHandler<string> WhiteMoveInput;
        event EventHandler<string> BlackMoveInput;
        event EventHandler<string> WhiteMoveNow;
        event EventHandler<string> BlackMoveNow;
        event EventHandler<string> StartSetup;
        event EventHandler<string> StopSetupWTM;
        event EventHandler<string> StopSetupBTM;
        event EventHandler<int> GameTypeChanged;
        event EventHandler<bool> AllowTakebacksChanged;
        event EventHandler WhiteTakeback;
        event EventHandler BlackTakeback;
    }

    public sealed class BoardEventSource : IBoardEventSource
    {
        public event EventHandler<string> StableBoard;
        public event EventHandler<string> Status;
        public event EventHandler<string> Scan;
        public event EventHandler<string> WClock;
        public event EventHandler<string> BClock;
        public event EventHandler<string> Result;
        public event EventHandler<string> NewGame;
        public event EventHandler<string> WhiteMoveInput;
        public event EventHandler<string> BlackMoveInput;
        public event EventHandler<string> WhiteMoveNow;
        public event EventHandler<string> BlackMoveNow;
        public event EventHandler<string> StartSetup;
        public event EventHandler<string> StopSetupWTM;
        public event EventHandler<string> StopSetupBTM;
        public event EventHandler<int> GameTypeChanged;
        public event EventHandler<bool> AllowTakebacksChanged;
        public event EventHandler WhiteTakeback;
        public event EventHandler BlackTakeback;

        // The native driver keeps raw function pointers, so these references must
        // live as long as the process does.
        private readonly DgtEbDllImport.CallbackStatusFunc _statusCallback;
        private readonly DgtEbDllImport.CallbackStableBoardFunc _stableBoardCallback;
        private readonly DgtEbDllImport.CallbackScanFunc _scanCallback;
        private readonly DgtEbDllImport.CallbackWClockFunc _wClockCallback;
        private readonly DgtEbDllImport.CallbackBClockFunc _bClockCallback;
        private readonly DgtEbDllImport.CallbackResultFunc _resultCallback;
        private readonly DgtEbDllImport.CallbackNewGameFunc _newGameCallback;
        private readonly DgtEbDllImport.CallbackWhiteMoveInputFunc _whiteMoveInputCallback;
        private readonly DgtEbDllImport.CallbackBlackMoveInputFunc _blackMoveInputCallback;
        private readonly DgtEbDllImport.CallbackWhiteMoveNowFunc _whiteMoveNowCallback;
        private readonly DgtEbDllImport.CallbackBlackMoveNowFunc _blackMoveNowCallback;
        private readonly DgtEbDllImport.CallbackStartSetupFunc _startSetupCallback;
        private readonly DgtEbDllImport.CallbackStopSetupWTMFunc _stopSetupWTMCallback;
        private readonly DgtEbDllImport.CallbackStopSetupBTMFunc _stopSetupBTMCallback;
        private readonly DgtEbDllImport.CallbackGameTypeChangedFunc _gameTypeChangedCallback;
        private readonly DgtEbDllImport.CallbackAllowTakebacksChangedFunc _allowTakebacksChangedCallback;
        private readonly DgtEbDllImport.CallbackWhiteTakebackFunc _whiteTakebackCallback;
        private readonly DgtEbDllImport.CallbackBlackTakebackFunc _blackTakebackCallback;

        public BoardEventSource()
        {
            _statusCallback = value => Status?.Invoke(this, value);
            _stableBoardCallback = value => StableBoard?.Invoke(this, value);
            _scanCallback = value => Scan?.Invoke(this, value);
            _wClockCallback = value => WClock?.Invoke(this, value);
            _bClockCallback = value => BClock?.Invoke(this, value);
            _resultCallback = value => Result?.Invoke(this, value);
            _newGameCallback = value => NewGame?.Invoke(this, value);
            _whiteMoveInputCallback = value => WhiteMoveInput?.Invoke(this, value);
            _blackMoveInputCallback = value => BlackMoveInput?.Invoke(this, value);
            _whiteMoveNowCallback = value => WhiteMoveNow?.Invoke(this, value);
            _blackMoveNowCallback = value => BlackMoveNow?.Invoke(this, value);
            _startSetupCallback = value => StartSetup?.Invoke(this, value);
            _stopSetupWTMCallback = value => StopSetupWTM?.Invoke(this, value);
            _stopSetupBTMCallback = value => StopSetupBTM?.Invoke(this, value);
            _gameTypeChangedCallback = value => GameTypeChanged?.Invoke(this, value);
            _allowTakebacksChangedCallback = value => AllowTakebacksChanged?.Invoke(this, value);
            _whiteTakebackCallback = () => WhiteTakeback?.Invoke(this, EventArgs.Empty);
            _blackTakebackCallback = () => BlackTakeback?.Invoke(this, EventArgs.Empty);

            _ = DgtEbDllImport.RegisterStableBoardFunc(_stableBoardCallback, IntPtr.Zero);
            _ = DgtEbDllImport.RegisterStatusFunc(_statusCallback, IntPtr.Zero);
            _ = DgtEbDllImport.RegisterCallbackScanFunc(_scanCallback, IntPtr.Zero);
            _ = DgtEbDllImport.RegisterWClockFunc(_wClockCallback, IntPtr.Zero);
            _ = DgtEbDllImport.RegisterBClockFunc(_bClockCallback, IntPtr.Zero);
            _ = DgtEbDllImport.RegisterResultFunc(_resultCallback, IntPtr.Zero);
            _ = DgtEbDllImport.RegisterNewGameFunc(_newGameCallback, IntPtr.Zero);
            _ = DgtEbDllImport.RegisterWhiteMoveInputFunc(_whiteMoveInputCallback, IntPtr.Zero);
            _ = DgtEbDllImport.RegisterBlackMoveInputFunc(_blackMoveInputCallback, IntPtr.Zero);
            _ = DgtEbDllImport.RegisterWhiteMoveNowFunc(_whiteMoveNowCallback, IntPtr.Zero);
            _ = DgtEbDllImport.RegisterBlackMoveNowFunc(_blackMoveNowCallback, IntPtr.Zero);
            _ = DgtEbDllImport.RegisterStartSetupFunc(_startSetupCallback, IntPtr.Zero);
            _ = DgtEbDllImport.RegisterStopSetupWTMFunc(_stopSetupWTMCallback, IntPtr.Zero);
            _ = DgtEbDllImport.RegisterStopSetupBTMFunc(_stopSetupBTMCallback, IntPtr.Zero);
            _ = DgtEbDllImport.RegisterGameTypeChangedFunc(_gameTypeChangedCallback, IntPtr.Zero);
            _ = DgtEbDllImport.RegisterAllowTakebacksChangedFunc(_allowTakebacksChangedCallback, IntPtr.Zero);
            _ = DgtEbDllImport.RegisterWhiteTakebackFunc(_whiteTakebackCallback, IntPtr.Zero);
            _ = DgtEbDllImport.RegisterBlackTakebackFunc(_blackTakebackCallback, IntPtr.Zero);
        }
    }
}
