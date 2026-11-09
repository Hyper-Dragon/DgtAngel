using System.Collections.Concurrent;
using UciComms.Data;

namespace UciComms
{
    public sealed class UciEngineManager : IUciEngineManager
    {
        public event Action? OnKibitzerActivated;
        public event Action? OnKibitzerDeactivated;
        public event Action<string>? OnKibitzerFenChange;

        // Subscribe to this events to get the parsed output from the engine
        public event EventHandler<UciResponse>? OnLoadedEngineOutputRecieved;

        // Subscribe to these events to get the raw output from the engine
        public event EventHandler<string?>? OnLoadedEngineOutputRecievedRaw;
        public event EventHandler<string?>? OnLoadedEngineErrorRecievedRaw;
        public event EventHandler<string?>? OnLoadedEngineInputSentRaw;

        public event Action<UciChessEngine?>? OnUciEngineLoaded;
        public event Action<string?>? OnUciEngineReleased;
        public event Action<string?>? OnUciEngineStartError;

        // Subscribe to this event to get the board evaluation from the engine
        public event Action<UciEngineEval>? OnLoadedEngineBoardEvalChanged;

        // Using a ConcurrentDictionary to ensure thread safety when accessing the UciEngines collection
        private readonly ConcurrentDictionary<string, UciChessEngine> UciEngines = new();

        private UciChessEngine? currentUciChessEngine;

        /// <summary>
        /// Whether the kibitzer has been switched on through <see cref="SwitchKibitzer"/>.
        /// </summary>
        /// <remarks>
        /// This is a one-way switch. Once it is on it stays on until Cherub is restarted, because
        /// the whole point is to make it impossible to have an engine watching a board that is
        /// being played on: if it could be switched off again, switching it off before making a
        /// move would put the engine back out of sight while still having been used.
        /// </remarks>
        public bool IsKibitzerRunning { get; private set; }

        public UciEngineManager() { }

        // RegisterEngine now returns a Task to allow for async operation
        public async Task RegisterEngineAsync(string engineRef, FileInfo engineExecutablePath)
        {
            // Adding a new UciChessEngine instance to the UciEngines collection
            _ = await Task.Run(() => UciEngines.TryAdd(engineRef, new UciChessEngine(engineExecutablePath)));
        }

        public async Task StopEngineAsync()
        {
            await Task.Run( () => { currentUciChessEngine?.Stop(); } );
        }

        // StartEngine now returns a Task to allow for async operation
        public async Task StartEngineAsync(string engineRef)
        {
            if (UciEngines.TryGetValue(engineRef, out UciChessEngine? value))
            {
                if (currentUciChessEngine != null)
                {
                    DetachEngineEvents(currentUciChessEngine);
                }

                currentUciChessEngine = value;

                AttachEngineEvents(currentUciChessEngine);

                await Task.Run(() => value?.StartUciEngine());
            }
        }

        /// <summary>
        /// Starts forwarding this engine's events to the manager's events.
        /// </summary>
        /// <remarks>
        /// Both the raw and the parsed output are forwarded. Only the raw one used to be, so
        /// OnLoadedEngineOutputRecieved had no producer and the UI's parsed-output handler - the
        /// one that fills the kibitzer info box and backs the "show UCI raw output" option - could
        /// never run.
        /// </remarks>
        internal void AttachEngineEvents(UciChessEngine engine)
        {
            // Detaching first makes this idempotent. Attaching the same engine twice would
            // otherwise subscribe every handler twice and deliver each event twice.
            DetachEngineEvents(engine);
            engine.OnOutputRecieved += EngOnOutputRecieved;
            engine.OnOutputRecievedRaw += EngOnOutputRecievedRawOut;
            engine.OnErrorRecievedRaw += EngOnOutputRecievedRawError;
            engine.OnInputSentRaw += EngOnOutputRecievedRawIn;
            engine.OnBoardEvalChanged += EngineOnBoardEvalChanged;
        }

        /// <summary>
        /// Stops forwarding this engine's events, so a replaced engine cannot keep driving the UI.
        /// </summary>
        internal void DetachEngineEvents(UciChessEngine engine)
        {
            engine.OnOutputRecieved -= EngOnOutputRecieved;
            engine.OnOutputRecievedRaw -= EngOnOutputRecievedRawOut;
            engine.OnErrorRecievedRaw -= EngOnOutputRecievedRawError;
            engine.OnInputSentRaw -= EngOnOutputRecievedRawIn;
            engine.OnBoardEvalChanged -= EngineOnBoardEvalChanged;
        }


        private void EngOnOutputRecieved(object? sender, UciResponse e)
        {
            OnLoadedEngineOutputRecieved?.Invoke(this, e);
        }

        private void EngOnOutputRecievedRawIn(object? sender, string? e)
        {
            OnLoadedEngineInputSentRaw?.Invoke(this, e);
        }

        private void EngOnOutputRecievedRawOut(object? sender, string? e)
        {
            OnLoadedEngineOutputRecievedRaw?.Invoke(this, e);
        }

        private void EngOnOutputRecievedRawError(object? sender, string? e)
        {
            OnLoadedEngineErrorRecievedRaw?.Invoke(this, e);
        }

        private void EngineOnBoardEvalChanged(UciEngineEval obj)
        {
            OnLoadedEngineBoardEvalChanged?.Invoke(obj);
        }

        public UciChessEngine? GetEngine(string engineRef)
        {
            _ = UciEngines.TryGetValue(engineRef, out UciChessEngine? value);
            return value;
        }

        public bool IsEngineRunning(string engineRef)
        {
            return UciEngines.ContainsKey(engineRef) && UciEngines[engineRef].IsRunning;
        }

        // UnRegisterEngine now returns a Task to allow for async operation
        public async Task UnRegisterEngineAsync(string engineRef)
        {
            if (UciEngines.TryGetValue(engineRef, out UciChessEngine? value))
            {
                await Task.Run(() => value?.StopUciEngine());
                _ = UciEngines.TryRemove(engineRef, out _);
            }
        }


        public string LoadedEngineAuthor => currentUciChessEngine?.EngineAuthor ?? "NO ENGINE";
        public string LoadedEngineName => currentUciChessEngine?.EngineName ?? "NO ENGINE";
        public FileInfo LoadedExecutable => currentUciChessEngine?.Executable ?? new FileInfo("");
        public bool LoadedEngineIsReady => currentUciChessEngine?.IsReady ?? false;
        public bool IsLoadedEngineRunning => currentUciChessEngine?.IsRunning ?? false;
        public bool IsLoadedEngineUciOk => currentUciChessEngine?.IsUciOk ?? false;
        public string LoadedEngineLastSeenFen => currentUciChessEngine?.LastSeenFen ?? "";
        public string LoadedEngineLastSeenMoveList => currentUciChessEngine?.LastSeenMoveList ?? "";
        public Dictionary<string, UciOption> LoadedEngineOptions => currentUciChessEngine?.Options ?? new Dictionary<string, UciOption>();

        public UciChessEngine? CurrentUciEngine { get; private set; }

        public void LoadedEngineGo(int depth) { currentUciChessEngine?.Go(depth); }
        public void LoadedEngineGoInfinite() { currentUciChessEngine?.GoInfinite(); }
        public void LoadedEngineNewGame() { currentUciChessEngine?.NewGame(); }
        public void LoadedEngineQuit() { currentUciChessEngine?.Quit(); }
        public void LoadedEngineSetDebug(bool debugOn) { currentUciChessEngine?.SetDebug(debugOn); }
        public void LoadedEngineSetMoves(string moves) { currentUciChessEngine?.SetMoves(moves); }
        public void LoadedEngineSetOption(string name, string value) { currentUciChessEngine?.SetOption(name, value); }
        public void LoadedEngineSetPosition(string fen) { currentUciChessEngine?.SetPosition(fen); }
        public void LoadedEngineStop() { currentUciChessEngine?.Stop(); }
        public bool LoadedEngineWaitForReady() { return currentUciChessEngine?.WaitForReady() ?? false;  }
        public bool LoadedEngineWaitForUciOk() { return currentUciChessEngine?.WaitForUciOk() ?? false;  }
        public async Task LoadEngineAsync(string exePath)
        {
            UciChessEngine? engSlot1 = GetEngine("KIB_ENG_SLOT_1");

            string slotKey = engSlot1 == null ? "KIB_ENG_SLOT_1" : "KIB_ENG_SLOT_2";
            string slotRemoveKey = engSlot1 == null ? "KIB_ENG_SLOT_2" : "KIB_ENG_SLOT_1";

            await RegisterEngineAsync(slotKey, new FileInfo(exePath));

            UciChessEngine engineNew = GetEngine(slotKey)
                ?? throw new InvalidOperationException($"The engine registered as [{slotKey}] was not found.");
            UciChessEngine? engineOld = GetEngine(slotRemoveKey);

            try
            {
                await StartEngineAsync(slotKey);

                if (IsLoadedEngineUciOk)
                {
                    _ = engineNew.WaitForReady();
                    //eng.SetDebug(false);
                }

                CurrentUciEngine = engineNew;
                OnUciEngineLoaded?.Invoke(engineNew);

                if (engineOld != null)
                {
                    OnUciEngineReleased?.Invoke($"{engineOld.EngineName} [{engineOld.EngineAuthor}]");
                    await UnRegisterEngineAsync(slotRemoveKey);
                }
            }
            catch (Exception ex)
            {
                await UnRegisterEngineAsync(slotKey);
                OnUciEngineStartError?.Invoke($"Failed to start engine :: {ex.Message} [{exePath}]");
            }
        }


        public void SwitchKibitzer(bool turnOn = false)
        {
            if (turnOn)
            {
                IsKibitzerRunning = true;
                // A kibitzer starts observing at the start of a game, which is the one point the
                // specification allows ucinewgame.
                currentUciChessEngine?.NewGame();
                OnKibitzerActivated?.Invoke();
                return;
            }

            // Refused: the switch only goes one way. Switching it off mid-session would let a
            // player use the engine and then hide it before moving.
            if (IsKibitzerRunning) { return; }

            CurrentUciEngine?.Stop();
            OnKibitzerDeactivated?.Invoke();
        }
    }
}
