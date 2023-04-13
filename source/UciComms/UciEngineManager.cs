using System.Collections.Concurrent;
using UciComms.Data;

namespace UciComms
{
    public sealed class UciEngineManager : IUciEngineManager
    {
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

        private UciChessEngine? currentUciChessEngine = null;

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
                    currentUciChessEngine.OnOutputRecievedRaw -= EngOnOutputRecievedRawOut;
                    currentUciChessEngine.OnErrorRecievedRaw -= EngOnOutputRecievedRawError;
                    currentUciChessEngine.OnInputSentRaw -= EngOnOutputRecievedRawIn;
                    currentUciChessEngine.OnBoardEvalChanged -= EngineOnBoardEvalChanged;
                }

                currentUciChessEngine = value;

                currentUciChessEngine.OnOutputRecievedRaw += EngOnOutputRecievedRawOut;
                currentUciChessEngine.OnErrorRecievedRaw += EngOnOutputRecievedRawError;
                currentUciChessEngine.OnInputSentRaw += EngOnOutputRecievedRawIn;
                currentUciChessEngine.OnBoardEvalChanged += EngineOnBoardEvalChanged;

                await Task.Run(() => value?.StartUciEngine());
            }
        }


        private void EngOnOutputRecievedRawIn(object sender, string e)
        {
            OnLoadedEngineInputSentRaw?.Invoke(this, e);
        }

        private void EngOnOutputRecievedRawOut(object sender, string e)
        {
            OnLoadedEngineOutputRecievedRaw?.Invoke(this, e);
        }

        private void EngOnOutputRecievedRawError(object sender, string e)
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

        public void LoadedEngineGo(int depth) { currentUciChessEngine?.Go(depth); }
        public void LoadedEngineGoInfinite() { currentUciChessEngine?.GoInfinite(); }
        public void LoadedEngineQuit() { currentUciChessEngine?.Quit(); }
        public void LoadedEngineSetDebug(bool debugOn) { currentUciChessEngine?.SetDebug(debugOn); }
        public void LoadedEngineSetMoves(string moves) { currentUciChessEngine?.SetMoves(moves); }
        public void LoadedEngineSetOption(string name, string value) { currentUciChessEngine?.SetOption(name, value); }
        public void LoadedEngineSetPosition(string fen) { currentUciChessEngine?.SetPosition(fen); }
        public void LoadedEngineStop() { currentUciChessEngine?.Stop(); }
        public bool LoadedEngineWaitForReady() { return currentUciChessEngine?.WaitForReady() ?? false;  }
        public bool LoadedEngineWaitForUciOk() { return currentUciChessEngine?.WaitForUciOk() ?? false;  }

    }
}