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

        // Only the callbacks the facade does not expose are registered against the native
        // driver from here.  The driver holds raw function pointers, so these references
        // must live as long as the process does.
        private readonly DgtEbDllImport.CallbackGameTypeChangedFunc _gameTypeChangedCallback;
        private readonly DgtEbDllImport.CallbackAllowTakebacksChangedFunc _allowTakebacksChangedCallback;
        private readonly DgtEbDllImport.CallbackWhiteTakebackFunc _whiteTakebackCallback;
        private readonly DgtEbDllImport.CallbackBlackTakebackFunc _blackTakebackCallback;

        public BoardEventSource(IDgtEbDllFacade facade)
        {
            // dgtebdll.dll keeps ONE function pointer per callback slot, so a second
            // registration of the same slot silently replaces the first.  This class used
            // to register StableBoard/Scan/clocks/moves itself, which took those slots away
            // from DgtEbDllAdapter.Init() the moment a gRPC client connected (this type is
            // created lazily on the first call) - the facade then went silent and with it
            // the LiveChess feed and Cherub's own board.
            //
            ArgumentNullException.ThrowIfNull(facade);

            // The facade owns the native registration; we just re-publish its events.
            facade.OnStatusMessage += (_, e) => Status?.Invoke(this, e.Message);
            facade.OnStableFenChanged += (_, e) => StableBoard?.Invoke(this, e.FEN);

            //The driver's scan callback is a separate stream with its own format, so it
            //must not be mixed into StableBoard - clients that render StableBoard choke on it.
            facade.OnFenChanged += (_, e) => Scan?.Invoke(this, e.FEN);
            facade.OnWClock += (_, e) => WClock?.Invoke(this, e.Message);
            facade.OnBClock += (_, e) => BClock?.Invoke(this, e.Message);
            facade.OnResult += (_, e) => Result?.Invoke(this, e.Message);
            facade.OnNewGame += (_, e) => NewGame?.Invoke(this, e.Message);
            facade.OnWhiteMoveInput += (_, e) => WhiteMoveInput?.Invoke(this, e.Message);
            facade.OnBlackMoveInput += (_, e) => BlackMoveInput?.Invoke(this, e.Message);
            facade.OnWhiteMoveNow += (_, e) => WhiteMoveNow?.Invoke(this, e.Message);
            facade.OnBlackMoveNow += (_, e) => BlackMoveNow?.Invoke(this, e.Message);
            facade.OnStartSetup += (_, e) => StartSetup?.Invoke(this, e.Message);
            facade.OnStopSetupWTM += (_, e) => StopSetupWTM?.Invoke(this, e.Message);
            facade.OnStopSetupBTM += (_, e) => StopSetupBTM?.Invoke(this, e.Message);

            _gameTypeChangedCallback = value => GameTypeChanged?.Invoke(this, value);
            _allowTakebacksChangedCallback = value => AllowTakebacksChanged?.Invoke(this, value);
            _whiteTakebackCallback = () => WhiteTakeback?.Invoke(this, EventArgs.Empty);
            _blackTakebackCallback = () => BlackTakeback?.Invoke(this, EventArgs.Empty);

            _ = DgtEbDllImport.RegisterGameTypeChangedFunc(_gameTypeChangedCallback, IntPtr.Zero);
            _ = DgtEbDllImport.RegisterAllowTakebacksChangedFunc(_allowTakebacksChangedCallback, IntPtr.Zero);
            _ = DgtEbDllImport.RegisterWhiteTakebackFunc(_whiteTakebackCallback, IntPtr.Zero);
            _ = DgtEbDllImport.RegisterBlackTakebackFunc(_blackTakebackCallback, IntPtr.Zero);
        }
    }
}
