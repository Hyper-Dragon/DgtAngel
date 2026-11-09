using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using UciComms;

namespace ChessHelpersTest
{
    /// <summary>
    /// Who owns the engine process, and who ends it.
    /// </summary>
    /// <remarks>
    /// An engine is a child process, and Windows does not end a child when its parent dies. Every
    /// route out of the application therefore has to end it explicitly, and an engine object has
    /// to hold exactly one process: these tests hold the parts that can be checked without an
    /// engine installed. The behaviour that is being pinned was seen for real - a killed Cherub
    /// left a Ruffian running with nothing pointing at it, still burning a core on an infinite
    /// search of a board nobody was looking at.
    /// </remarks>
    [TestClass]
    public class UciEngineProcessTests
    {
        private static UciChessEngine NewEngine()
        {
            // The constructor does not open the file and nothing here starts a process.
            return new UciChessEngine(new FileInfo("not-a-real-engine.exe"));
        }

        private static Process ExitedProcess()
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "dotnet",
                Arguments = "--version",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            };

            Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("could not start a helper process");
            _ = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return process;
        }

        [TestMethod(DisplayName = "An engine with no process attached is not running")]
        public void NoProcessIsNotAlive()
        {
            Assert.IsFalse(NewEngine().IsProcessAlive);
        }

        [TestMethod(DisplayName = "A live process is running")]
        public void LiveProcessIsAlive()
        {
            UciChessEngine engine = NewEngine();
            engine.RunningProcess = Process.GetCurrentProcess();

            Assert.IsTrue(engine.IsProcessAlive);

            // Deliberately not shut down: this is the test runner's own process.
            engine.RunningProcess = null;
        }

        [TestMethod(DisplayName = "A process that has already exited is not running")]
        public void ExitedProcessIsNotAlive()
        {
            using Process exited = ExitedProcess();
            UciChessEngine engine = NewEngine();
            engine.RunningProcess = exited;

            Assert.IsFalse(engine.IsProcessAlive, "a process that has already gone is not something to start a second one over");
        }

        [TestMethod(DisplayName = "Starting an engine that is already running does not replace its process")]
        public void StartDoesNotReplaceALiveProcess()
        {
            UciChessEngine engine = NewEngine();
            Process alive = Process.GetCurrentProcess();
            engine.RunningProcess = alive;

            // Without the guard this launches not-a-real-engine.exe, so the test fails either on
            // the missing executable or on the process having been replaced. Both mean the same
            // thing: the engine's own process had been abandoned.
            engine.StartUciEngine();

            Assert.AreSame(alive, engine.RunningProcess, "starting twice must not leave the first process with nothing holding it");

            engine.RunningProcess = null;
        }

        [TestMethod(DisplayName = "Shutting down an engine with no process is harmless")]
        public void ShutdownWithoutAProcessIsHarmless()
        {
            UciChessEngine engine = NewEngine();

            engine.Shutdown();

            Assert.IsNull(engine.RunningProcess, "there was nothing to end, and nothing is left attached");
        }

        [TestMethod(DisplayName = "Shutting down releases a process that has already exited")]
        public void ShutdownReleasesAnExitedProcess()
        {
            UciChessEngine engine = NewEngine();
            engine.RunningProcess = ExitedProcess();

            engine.Shutdown();

            Assert.IsNull(engine.RunningProcess, "an exited process is disposed of rather than left attached");
        }

        [TestMethod(DisplayName = "Stopping every engine leaves the manager holding none")]
        public void StopAllEnginesLeavesNothingBehind()
        {
            UciEngineManager manager = new();

            manager.StopAllEngines();

            Assert.IsNull(manager.CurrentUciEngine, "no engine is loaded after a shutdown");
            Assert.AreEqual("NO ENGINE", manager.LoadedEngineName);
        }
    }
}
