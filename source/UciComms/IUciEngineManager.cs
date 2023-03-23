namespace UciComms
{
    public interface IUciEngineManager
    {
        bool IsEngineRunning(string engineRef);
        void StartEngine(string engineRef);
        UciChessEngine? GetEngine(string engineRef);
        void RegisterEngine(string engineRef, FileInfo engineExecutablePath);
        void UnRegisterEngine(string engineRef);
    }
}