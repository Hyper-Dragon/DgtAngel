using System.Collections.Concurrent;

namespace UciComms
{


    // Implementing the IUciEngineManager interface to ensure compatibility with existing code
    public sealed class UciEngineManager : IUciEngineManager
    {
        // Using a ConcurrentDictionary to ensure thread safety when accessing the UciEngines collection
        private readonly ConcurrentDictionary<string, UciChessEngine> UciEngines = new();

        public UciEngineManager() { }

        // RegisterEngine now returns a Task to allow for async operation
        public async Task RegisterEngineAsync(string engineRef, FileInfo engineExecutablePath)
        {
            // Adding a new UciChessEngine instance to the UciEngines collection
            _ = await Task.Run(() => UciEngines.TryAdd(engineRef, new UciChessEngine(engineExecutablePath)));
        }

        // StartEngine now returns a Task to allow for async operation
        public async Task StartEngineAsync(string engineRef)
        {
            if (UciEngines.TryGetValue(engineRef, out UciChessEngine? value))
            {
                await Task.Run(() => value?.StartUciEngine());
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

        // UnRegisterEngine now returns a Task to allow for async operation
        public async Task UnRegisterEngineAsync(string engineRef)
        {
            if (UciEngines.TryGetValue(engineRef, out UciChessEngine? value))
            {
                await Task.Run(() => value?.StopUciEngine());
                _ = UciEngines.TryRemove(engineRef, out _);
            }
        }
    }
}