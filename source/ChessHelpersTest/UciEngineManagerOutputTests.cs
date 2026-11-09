using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using UciComms;
using UciComms.Data;

namespace ChessHelpersTest
{
    /// <summary>
    /// The manager's forwarding of engine output.
    /// </summary>
    /// <remarks>
    /// The manager used to forward only the raw output events, so <c>OnLoadedEngineOutputRecieved</c>
    /// - which the UI subscribes to for the kibitzer info box and the "show UCI raw output" option -
    /// had no producer at all. It could never fire, and nothing failed to say so.
    /// </remarks>
    [TestClass]
    public class UciEngineManagerOutputTests
    {
        private static (UciEngineManager Manager, UciChessEngine Engine) NewWiredPair()
        {
            UciEngineManager manager = new();
            UciChessEngine engine = new(new FileInfo("not-a-real-engine.exe"));
            manager.AttachEngineEvents(engine);

            return (manager, engine);
        }

        [TestMethod(DisplayName = "A parsed engine line reaches the manager")]
        public void ParsedOutputReachesTheManager()
        {
            (UciEngineManager manager, UciChessEngine engine) = NewWiredPair();

            UciResponse? seen = null;
            manager.OnLoadedEngineOutputRecieved += (_, response) => seen = response;

            engine.HandleOutputLine("info depth 6 score cp 27 hashfull 0 time 17 nodes 9627 pv b1c3 b8c6");

            Assert.IsNotNull(seen, "the parsed output event must fire - this is the one that had no producer");
            Assert.IsInstanceOfType<InfoResponse>(seen);
            Assert.AreEqual(6, ((InfoResponse)seen).Depth);
        }

        [TestMethod(DisplayName = "The raw engine line reaches the manager")]
        public void RawOutputReachesTheManager()
        {
            (UciEngineManager manager, UciChessEngine engine) = NewWiredPair();

            List<string?> raw = new();
            manager.OnLoadedEngineOutputRecievedRaw += (_, line) => raw.Add(line);

            engine.HandleOutputLine("uciok");

            CollectionAssert.Contains(raw, "uciok", "the raw event must still carry the untouched line");
        }

        [TestMethod(DisplayName = "An id line names the engine")]
        public void IdLineNamesTheEngine()
        {
            (_, UciChessEngine engine) = NewWiredPair();

            engine.HandleOutputLine("id name Rybka 2.3.2a mp 32-bit");
            engine.HandleOutputLine("id author Vasik Rajlich");

            Assert.AreEqual("Rybka 2.3.2a mp 32-bit", engine.EngineName);
            Assert.AreEqual("Vasik Rajlich", engine.EngineAuthor);
        }

        [TestMethod(DisplayName = "The handshake flags are set from the engine's answers")]
        public void HandshakeFlagsAreSet()
        {
            (_, UciChessEngine engine) = NewWiredPair();

            Assert.IsFalse(engine.IsUciOk, "a new engine has not answered uci yet");

            engine.HandleOutputLine("uciok");
            engine.HandleOutputLine("readyok");

            Assert.IsTrue(engine.IsUciOk, "uciok must set the handshake flag");
            Assert.IsTrue(engine.IsReady, "readyok must set the ready flag");
        }

        [TestMethod(DisplayName = "An id line is forwarded as a parsed response too")]
        public void IdLineIsForwarded()
        {
            (UciEngineManager manager, UciChessEngine engine) = NewWiredPair();

            UciResponse? seen = null;
            manager.OnLoadedEngineOutputRecieved += (_, response) => seen = response;

            engine.HandleOutputLine("id name Rybka 2.3.2a mp 32-bit");

            Assert.IsInstanceOfType<IdResponse>(seen, "id lines are parsed responses, so they must be forwarded");
            Assert.AreEqual("Rybka 2.3.2a mp 32-bit", ((IdResponse)seen).Name);
        }

        [TestMethod(DisplayName = "A detached engine stops driving the manager")]
        public void DetachedEngineStopsForwarding()
        {
            (UciEngineManager manager, UciChessEngine engine) = NewWiredPair();
            manager.DetachEngineEvents(engine);

            int seen = 0;
            manager.OnLoadedEngineOutputRecieved += (_, _) => seen++;

            engine.HandleOutputLine("info depth 6 score cp 27 pv b1c3");

            Assert.AreEqual(0, seen, "a replaced engine must not keep feeding the UI");
        }

        [TestMethod(DisplayName = "Attaching the same engine twice delivers each line once")]
        public void AttachingTwiceDeliversOnce()
        {
            (UciEngineManager manager, UciChessEngine engine) = NewWiredPair();
            manager.AttachEngineEvents(engine);

            int seen = 0;
            manager.OnLoadedEngineOutputRecieved += (_, _) => seen++;

            engine.HandleOutputLine("info depth 6 score cp 27 pv b1c3");

            Assert.AreEqual(1, seen, "re-attaching must not double every event");
        }

        [TestMethod(DisplayName = "An unparsed line is not forwarded as a parsed response")]
        public void UnparsedLineIsNotForwarded()
        {
            (UciEngineManager manager, UciChessEngine engine) = NewWiredPair();

            int seen = 0;
            manager.OnLoadedEngineOutputRecieved += (_, _) => seen++;

            engine.HandleOutputLine("some engine chatter that is not UCI");

            Assert.AreEqual(0, seen, "only recognised responses reach the parsed event");
        }
    }
}
