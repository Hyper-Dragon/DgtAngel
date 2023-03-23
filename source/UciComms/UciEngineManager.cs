namespace UciComms
{

    public sealed class UciEngineManager : IUciEngineManager
    {
        private readonly Dictionary<string, UciChessEngine> UciEngines = new();

        public UciEngineManager() { }

        public void RegisterEngine(string engineRef, FileInfo engineExecutablePath)
        {
            UciEngines.Add(engineRef, new UciChessEngine(engineExecutablePath));
        }

        public void StartEngine(string engineRef)
        {
            if (UciEngines.TryGetValue(engineRef, out UciChessEngine? value))
            {
                value?.StartUciEngine();
            }
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

        public void UnRegisterEngine(string engineRef)
        {
            if (UciEngines.TryGetValue(engineRef, out UciChessEngine? value))
            {
                value?.StopUciEngine();
                _ = UciEngines.Remove(engineRef);
            }
        }
    }
}
