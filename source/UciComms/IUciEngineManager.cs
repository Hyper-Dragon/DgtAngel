namespace UciComms
{
    public interface IUciEngineManager
    {
        UciChessEngine? GetEngine(string engineRef);
        bool IsEngineRunning(string engineRef);
        Task RegisterEngineAsync(string engineRef, FileInfo engineExecutablePath);
        Task StartEngineAsync(string engineRef);
        Task UnRegisterEngineAsync(string engineRef);
    }
}