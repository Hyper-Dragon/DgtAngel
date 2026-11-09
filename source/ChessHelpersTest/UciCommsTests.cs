using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using UciComms;
using UciComms.Data;

namespace ChessHelpersTest
{
    /// <summary>
    /// Parsing of engine output. Expectations come from the UCI specification, not from
    /// what the parser happens to return - the whole point of this code is to read someone
    /// else's text correctly, so agreeing with itself proves nothing.
    /// </summary>
    [TestClass]
    public class UciChessEngineParsingTests
    {
        private static UciChessEngine _engine = null!;

        [ClassInitialize]
        public static void ClassSetup(TestContext context)
        {
            // Constructing an engine costs nothing on its own (no process is started), but
            // it does start the eval consumer, so one shared instance is cheaper than many.
            _ = context;
            _engine = new UciChessEngine(new FileInfo("not-a-real-engine.exe"));
        }

        // ------------------------------------------------------------ handshake

        [TestMethod(DisplayName = "Blank lines are not responses")]
        [DataRow("")]
        [DataRow("   ")]
        public void BlankLinesAreIgnored(string line)
        {
            Assert.IsNull(_engine.ParseResponse(line));
        }

        [TestMethod(DisplayName = "uciok and readyok are recognised")]
        public void HandshakeResponses()
        {
            Assert.IsInstanceOfType(_engine.ParseResponse("uciok"), typeof(UciOkResponse));
            Assert.IsInstanceOfType(_engine.ParseResponse("readyok"), typeof(ReadyOkResponse));
        }

        [TestMethod(DisplayName = "The raw line is preserved on every response")]
        public void RawLineIsPreserved()
        {
            UciResponse? response = _engine.ParseResponse("uciok");

            Assert.IsNotNull(response);
            Assert.AreEqual("uciok", response.RawData);
        }

        // ----------------------------------------------------------------- id

        [TestMethod(DisplayName = "The engine name is read in full, spaces included")]
        public void EngineNameIsRead()
        {
            IdResponse? response = _engine.ParseResponse("id name Stockfish 16.1") as IdResponse;

            Assert.IsNotNull(response);
            Assert.AreEqual("Stockfish 16.1", response.Name);
        }

        [TestMethod(DisplayName = "The engine author is read")]
        public void EngineAuthorIsRead()
        {
            IdResponse? response = _engine.ParseResponse("id author the Stockfish developers") as IdResponse;

            Assert.IsNotNull(response);
            Assert.AreEqual("the Stockfish developers", response.Author);
        }

        [TestMethod(DisplayName = "A combined id line keeps name and author apart")]
        public void CombinedIdLine()
        {
            IdResponse? response = _engine.ParseResponse("id name EngineX author Alice") as IdResponse;

            Assert.IsNotNull(response);
            Assert.AreEqual("EngineX", response.Name, "the name must not swallow the author section");
            Assert.AreEqual("Alice", response.Author);
        }

        // ------------------------------------------------------------- option

        [TestMethod(DisplayName = "A spin option is read with its bounds")]
        public void SpinOption()
        {
            _ = _engine.ParseResponse("option name Hash type spin default 16 min 1 max 1024");

            Assert.IsTrue(_engine.Options.ContainsKey("Hash"));
            UciOption option = _engine.Options["Hash"];
            Assert.AreEqual("spin", option.Type);
            Assert.AreEqual("16", option.DefaultValue);
            Assert.AreEqual("1", option.MinValue);
            Assert.AreEqual("1024", option.MaxValue);
        }

        [TestMethod(DisplayName = "A check option is read")]
        public void CheckOption()
        {
            _ = _engine.ParseResponse("option name UCI_Chess960 type check default false");

            Assert.IsTrue(_engine.Options.ContainsKey("UCI_Chess960"));
            Assert.AreEqual("check", _engine.Options["UCI_Chess960"].Type);
        }

        /// <summary>
        /// UCI option names routinely contain spaces - "Move Overhead", "Slow Mover",
        /// "Skill Level". The name is everything between "name" and "type".
        /// </summary>
        [TestMethod(DisplayName = "An option name containing spaces is read in full")]
        public void OptionNameWithSpaces()
        {
            _ = _engine.ParseResponse("option name Move Overhead type spin default 30 min 0 max 5000");

            Assert.IsTrue(
                _engine.Options.ContainsKey("Move Overhead"),
                $"expected the full name, got: {string.Join(" | ", _engine.Options.Keys)}");
        }

        // ----------------------------------------------------------- bestmove

        [TestMethod(DisplayName = "A best move is read")]
        public void BestMove()
        {
            BestMoveResponse? response = _engine.ParseResponse("bestmove e2e4") as BestMoveResponse;

            Assert.IsNotNull(response);
            Assert.AreEqual("e2e4", response.BestMove);
            Assert.AreEqual("", response.PonderMove);
        }

        [TestMethod(DisplayName = "A ponder move is read")]
        public void BestMoveWithPonder()
        {
            BestMoveResponse? response = _engine.ParseResponse("bestmove e2e4 ponder e7e5") as BestMoveResponse;

            Assert.IsNotNull(response);
            Assert.AreEqual("e2e4", response.BestMove);
            Assert.AreEqual("e7e5", response.PonderMove);
        }

        /// <summary>
        /// Engines do emit a bare "bestmove" when they have no move (no legal moves in a
        /// position they were asked to search). It must not take the parser down.
        /// </summary>
        [TestMethod(DisplayName = "A bestmove with no argument does not throw")]
        public void BareBestMove()
        {
            BestMoveResponse? response = _engine.ParseResponse("bestmove") as BestMoveResponse;

            Assert.IsNotNull(response);
            Assert.AreEqual("", response.BestMove);
        }

        // --------------------------------------------------------------- info

        [TestMethod(DisplayName = "Info fields are read")]
        public void InfoFields()
        {
            InfoResponse? response = _engine.ParseResponse(
                "info depth 12 seldepth 20 multipv 1 nodes 123456 nps 5000 time 100 hashfull 12 tbhits 3 sbhits 4 cpuload 55") as InfoResponse;

            Assert.IsNotNull(response);
            Assert.AreEqual(12, response.Depth);
            Assert.AreEqual(20, response.SelDepth);
            Assert.AreEqual(1, response.MultiPv);
            Assert.AreEqual(123456, response.Nodes);
            Assert.AreEqual(5000, response.Nps);
            Assert.AreEqual(100, response.Time);
            Assert.AreEqual(12, response.HashFull);
            Assert.AreEqual(3, response.TbHits);
            Assert.AreEqual(4, response.SbHits);
            Assert.AreEqual(55, response.CpuLoad);
        }

        [TestMethod(DisplayName = "The principal variation is read in full")]
        public void PrincipalVariation()
        {
            InfoResponse? response = _engine.ParseResponse("info depth 8 pv e2e4 e7e5 g1f3") as InfoResponse;

            Assert.IsNotNull(response);
            Assert.AreEqual("e2e4 e7e5 g1f3", response.Pv);
        }

        [TestMethod(DisplayName = "A free-text info line is read in full")]
        public void InfoString()
        {
            InfoResponse? response = _engine.ParseResponse("info string NNUE evaluation using nn-1234.nnue") as InfoResponse;

            Assert.IsNotNull(response);
            Assert.AreEqual("NNUE evaluation using nn-1234.nnue", response.StringInfo);
        }

        [TestMethod(DisplayName = "An unrecognised line is not a response")]
        public void UnrecognisedLine()
        {
            Assert.IsNull(_engine.ParseResponse("something else entirely"));
        }
    }

    /// <summary>
    /// The score/depth/PV readers behind the evaluation display. They scan engine text, and
    /// engine text contains tokens that merely start with the ones we look for.
    /// </summary>
    [TestClass]
    public class UciEvalParsingTests
    {
        [TestMethod(DisplayName = "A centipawn score is read")]
        public void CentipawnScore()
        {
            (int? cp, int mateIn) = Eval.GetBoardEvalFromLine("info depth 12 score cp 34 nodes 100 pv e2e4");

            Assert.AreEqual(34, cp);
            Assert.AreEqual(0, mateIn);
        }

        [TestMethod(DisplayName = "A negative centipawn score is read")]
        public void NegativeCentipawnScore()
        {
            (int? cp, _) = Eval.GetBoardEvalFromLine("info depth 12 score cp -250 pv e2e4");

            Assert.AreEqual(-250, cp);
        }

        [TestMethod(DisplayName = "A mate score is converted to a winning score")]
        public void MateScore()
        {
            (int? cp, int mateIn) = Eval.GetBoardEvalFromLine("info depth 20 score mate 3 pv e2e4");

            Assert.AreEqual(3, mateIn);
            Assert.IsNotNull(cp);
            Assert.IsTrue(cp > 0, "a positive mate means the side to move wins");
        }

        [TestMethod(DisplayName = "A mate against is converted to a losing score")]
        public void MateAgainst()
        {
            (int? cp, int mateIn) = Eval.GetBoardEvalFromLine("info depth 20 score mate -2 pv e2e4");

            Assert.AreEqual(-2, mateIn);
            Assert.IsNotNull(cp);
            Assert.IsTrue(cp < 0, "a negative mate means the side to move is being mated");
        }

        [TestMethod(DisplayName = "A line with no score reports no score")]
        public void NoScore()
        {
            (int? cp, _) = Eval.GetBoardEvalFromLine("info depth 12 nodes 100 pv e2e4");

            Assert.IsNull(cp);
        }

        /// <summary>
        /// "cpuload" is a standard UCI info token and contains the letters "cp". A line
        /// carrying only cpu load has no score and must not be read as one.
        /// </summary>
        [TestMethod(DisplayName = "cpuload is not mistaken for a centipawn score")]
        public void CpuLoadIsNotAScore()
        {
            (int? cp, _) = Eval.GetBoardEvalFromLine("info depth 12 cpuload 55 nodes 100");

            Assert.IsNull(cp, "there is no score in this line");
        }

        [TestMethod(DisplayName = "Depth is read")]
        public void DepthIsRead()
        {
            Assert.AreEqual(12, Eval.GetDepthFromLine("info depth 12 seldepth 20 pv e2e4"));
        }

        /// <summary>
        /// "seldepth" contains "depth". A line with only a selective depth has no depth.
        /// </summary>
        [TestMethod(DisplayName = "seldepth is not mistaken for depth")]
        public void SelDepthIsNotDepth()
        {
            Assert.IsNull(Eval.GetDepthFromLine("info seldepth 20 nodes 100"), "there is no depth in this line");
        }

        [TestMethod(DisplayName = "The best move is taken from the principal variation")]
        public void BestMoveFromPv()
        {
            Assert.AreEqual("e2e4", Eval.GetBestMoveFromLine("info depth 12 pv e2e4 e7e5"));
        }

        /// <summary>A principal variation is not always four characters long.</summary>
        [TestMethod(DisplayName = "A short principal variation does not throw")]
        public void ShortPv()
        {
            Assert.AreEqual("e4", Eval.GetBestMoveFromLine("info depth 1 pv e4"));
        }

        [TestMethod(DisplayName = "A line with no principal variation reports no best move")]
        public void NoPv()
        {
            Assert.IsNull(Eval.GetBestMoveFromLine("info depth 12 nodes 100"));
        }
    }
}
