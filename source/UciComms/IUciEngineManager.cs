using UciComms.Data;

namespace UciComms
{
    public interface IUciEngineManager
    {
        event Action OnKibitzerActivated;
        event Action OnKibitzerDeactivated;
        event Action<string> OnKibitzerFenChange;

        // Subscribe to this events to get the parsed output from the engine
        event EventHandler<UciResponse>? OnLoadedEngineOutputRecieved;

        // Subscribe to these events to get the raw output from the engine
        event EventHandler<string?>? OnLoadedEngineOutputRecievedRaw;
        event EventHandler<string?>? OnLoadedEngineErrorRecievedRaw;
        event EventHandler<string?>? OnLoadedEngineInputSentRaw;

        // Subscribe to this event to get the board evaluation from the engine
        event Action<UciEngineEval>? OnLoadedEngineBoardEvalChanged;

        event Action<UciChessEngine?>? OnUciEngineLoaded;
        event Action<string?>? OnUciEngineReleased;
        event Action<string?>? OnUciEngineStartError;

        void LoadedEngineGo(int depth);
        void LoadedEngineGoInfinite();
        string LoadedEngineAuthor { get; }
        string LoadedEngineName { get; }
        FileInfo LoadedExecutable { get; }
        bool LoadedEngineIsReady { get; }
        bool IsLoadedEngineRunning { get; }
        bool IsLoadedEngineUciOk { get; }
        string LoadedEngineLastSeenFen { get; }
        string LoadedEngineLastSeenMoveList { get; }
        Dictionary<string, UciOption> LoadedEngineOptions { get; }

        UciChessEngine? CurrentUciEngine { get; }

        Task LoadEngineAsync(string exePath);

        void SwitchKibitzer(bool turnOn);

        void LoadedEngineQuit();
        void LoadedEngineSetDebug(bool debugOn);
        void LoadedEngineSetMoves(string moves);
        void LoadedEngineSetOption(string name, string value);
        void LoadedEngineSetPosition(string fen);
        void LoadedEngineStop();
        bool LoadedEngineWaitForReady();
        bool LoadedEngineWaitForUciOk();


        UciChessEngine? GetEngine(string engineRef);
        bool IsEngineRunning(string engineRef);
        Task RegisterEngineAsync(string engineRef, FileInfo engineExecutablePath);
        Task StartEngineAsync(string engineRef);
        Task StopEngineAsync();
        Task UnRegisterEngineAsync(string engineRef);
    }
}