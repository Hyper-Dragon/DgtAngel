using DgtCherub.Services;
using DgtCherubTest.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UciComms;

namespace DgtCherubTest
{
    /// <summary>
    /// The anti-cheat rules of the kibitzer, and the resilience of the board pipeline they live in.
    /// </summary>
    /// <remarks>
    /// Two things are being held down here.
    ///
    /// First: an engine may be loaded, but it is not told about the board unless the kibitzer has
    /// been switched on. That is the whole point of the switch, and it is a rule about what is
    /// *not* sent, so nothing else would notice it going wrong.
    ///
    /// Second: the local board processor is the only thing moving the board forward. A fault in it
    /// takes the board picture, every clock page and the engine with it, and does so silently -
    /// which is exactly what happened when a subscriber threw.
    /// </remarks>
    [TestClass]
    public class KibitzerBoardGateTests
    {
        private const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";
        private const string AfterE4Fen = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR";

        private static AngelHubService CreateHub(Mock<IUciEngineManager> uci, out InstantTimeProvider clock)
        {
            clock = new InstantTimeProvider();

            return new AngelHubService(NullLogger<AngelHubService>.Instance, uci.Object) { TimeSource = clock };
        }

        private static Mock<IUciEngineManager> EngineManager(bool kibitzerRunning)
        {
            Mock<IUciEngineManager> uci = new(MockBehavior.Loose);
            _ = uci.SetupGet(m => m.IsKibitzerRunning).Returns(kibitzerRunning);

            return uci;
        }

        [TestMethod(DisplayName = "With the kibitzer off the engine is never given the board")]
        public async Task KibitzerOffSendsNothingToTheEngine()
        {
            Mock<IUciEngineManager> uci = EngineManager(kibitzerRunning: false);
            AngelHubService hub = CreateHub(uci, out _);

            hub.LocalBoardUpdate(StartFen);
            hub.LocalBoardUpdate(AfterE4Fen);

            await hub.DrainAsync().WaitAsync(TimeSpan.FromSeconds(10));

            uci.Verify(m => m.LoadedEngineSetPosition(It.IsAny<string>()), Times.Never,
                "a loaded engine must not be fed positions while the kibitzer is off");
            uci.Verify(m => m.LoadedEngineGoInfinite(), Times.Never);
            uci.Verify(m => m.LoadedEngineStop(), Times.Never);
        }

        [TestMethod(DisplayName = "With the kibitzer on the engine is given each position")]
        public async Task KibitzerOnSendsTheBoard()
        {
            Mock<IUciEngineManager> uci = EngineManager(kibitzerRunning: true);
            AngelHubService hub = CreateHub(uci, out _);

            hub.LocalBoardUpdate(StartFen);
            hub.LocalBoardUpdate(AfterE4Fen);

            await hub.DrainAsync().WaitAsync(TimeSpan.FromSeconds(10));

            uci.Verify(m => m.LoadedEngineSetPosition(It.Is<string>(fen => fen.StartsWith(StartFen, StringComparison.Ordinal))), Times.AtLeastOnce,
                "the first position on the board must reach the engine");
            uci.Verify(m => m.LoadedEngineSetPosition(It.Is<string>(fen => fen.StartsWith(AfterE4Fen, StringComparison.Ordinal))), Times.AtLeastOnce,
                "and so must the one after it");
            uci.Verify(m => m.LoadedEngineGoInfinite(), Times.AtLeast(2));
        }

        [TestMethod(DisplayName = "Switching the kibitzer off cannot stop a position that is already on its way")]
        public async Task KibitzerOffAfterOnSendsNothingNew()
        {
            Mock<IUciEngineManager> uci = EngineManager(kibitzerRunning: true);
            AngelHubService hub = CreateHub(uci, out _);

            hub.LocalBoardUpdate(StartFen);
            await hub.DrainAsync().WaitAsync(TimeSpan.FromSeconds(10));

            // The latch makes this impossible through the interface; the point is that even if the
            // state were changed, no further board is sent.
            uci.SetupGet(m => m.IsKibitzerRunning).Returns(false);
            uci.Invocations.Clear();

            hub.LocalBoardUpdate(AfterE4Fen);
            await hub.DrainAsync().WaitAsync(TimeSpan.FromSeconds(10));

            uci.Verify(m => m.LoadedEngineSetPosition(It.IsAny<string>()), Times.Never);
        }

        [TestMethod(DisplayName = "A subscriber that throws does not stop the board updating")]
        public async Task ProcessorSurvivesAThrowingSubscriber()
        {
            Mock<IUciEngineManager> uci = EngineManager(kibitzerRunning: false);
            AngelHubService hub = CreateHub(uci, out _);

            int seen = 0;
            hub.OnLocalFenChange += _ =>
            {
                seen++;
                throw new InvalidOperationException("a subscriber blew up");
            };

            hub.LocalBoardUpdate(StartFen);
            await hub.DrainAsync().WaitAsync(TimeSpan.FromSeconds(10));

            // The whole board pipeline hangs off this loop: a fault here silently freezes the board
            // picture, every clock page and the engine on the last position for the rest of the run.
            hub.LocalBoardUpdate(AfterE4Fen);
            await hub.DrainAsync().WaitAsync(TimeSpan.FromSeconds(10));

            Assert.AreEqual(2, seen, "both positions must be processed even though the first one threw");
            Assert.AreEqual(AfterE4Fen, hub.LocalBoardFEN, "the board must have moved on to the second position");
        }

        [TestMethod(DisplayName = "A position that cannot be inferred does not stop the board updating")]
        public async Task ProcessorSurvivesABadPosition()
        {
            Mock<IUciEngineManager> uci = EngineManager(kibitzerRunning: false);
            AngelHubService hub = CreateHub(uci, out _);

            // Not a board the chess library will accept, so anything that parses it will fail.
            hub.LocalBoardUpdate("this is not a board");
            await hub.DrainAsync().WaitAsync(TimeSpan.FromSeconds(10));

            hub.LocalBoardUpdate(StartFen);
            await hub.DrainAsync().WaitAsync(TimeSpan.FromSeconds(10));

            Assert.AreEqual(StartFen, hub.LocalBoardFEN,
                "the processor must keep going after a position it cannot handle");
        }

        [TestMethod(DisplayName = "A pawn lifted and placed two squares on still opens en passant")]
        public async Task EnPassantSurvivesThePieceInHandScan()
        {
            Mock<IUciEngineManager> uci = EngineManager(kibitzerRunning: false);
            AngelHubService hub = CreateHub(uci, out _);

            // What the board actually reports for b7-b5: the settled position, the pawn in the
            // air, then the pawn on b5. Measuring the move from the middle scan loses the double
            // push, and with it the en passant square the engine is entitled to.
            hub.LocalBoardUpdate(StartFen);
            hub.LocalBoardUpdate("rnbqkbnr/p1pppppp/8/8/8/8/PPPPPPPP/RNBQKBNR");
            hub.LocalBoardUpdate("rnbqkbnr/p1pppppp/8/1p6/8/8/PPPPPPPP/RNBQKBNR");

            await hub.DrainAsync().WaitAsync(TimeSpan.FromSeconds(10));

            Assert.IsNotNull(hub.CurrentKibitzerPosition);
            Assert.AreEqual("b6", hub.CurrentKibitzerPosition.EnPassantSquare,
                "black's pawn crossed b6, so white may capture en passant there");
            Assert.IsTrue(hub.CurrentKibitzerPosition.IsWhiteToPlay, "black has just moved");
        }

        [TestMethod(DisplayName = "The en passant square is in the FEN the engine is given")]
        public async Task EngineFenCarriesTheEnPassantSquare()
        {
            Mock<IUciEngineManager> uci = EngineManager(kibitzerRunning: true);
            AngelHubService hub = CreateHub(uci, out _);

            hub.LocalBoardUpdate(StartFen);
            hub.LocalBoardUpdate("rnbqkbnr/p1pppppp/8/8/8/8/PPPPPPPP/RNBQKBNR");
            hub.LocalBoardUpdate("rnbqkbnr/p1pppppp/8/1p6/8/8/PPPPPPPP/RNBQKBNR");

            await hub.DrainAsync().WaitAsync(TimeSpan.FromSeconds(10));

            // The whole point of remembering the move: the engine cannot see a capture it is not
            // told about. The fourth field of the FEN is the one that says so.
            uci.Verify(m => m.LoadedEngineSetPosition(It.Is<string>(fen =>
                    fen.StartsWith("rnbqkbnr/p1pppppp/8/1p6/8/8/PPPPPPPP/RNBQKBNR w ", StringComparison.Ordinal) &&
                    fen.Split(' ')[3] == "b6")),
                Times.Once,
                "the position sent to the engine must carry the en passant square");
        }

        [TestMethod(DisplayName = "A board with a piece in the air is never sent to the engine")]
        public async Task PieceInHandIsNotSentToTheEngine()
        {
            Mock<IUciEngineManager> uci = EngineManager(kibitzerRunning: true);
            AngelHubService hub = CreateHub(uci, out _);

            const string lifted = "rnbqkbnr/p1pppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";

            hub.LocalBoardUpdate(StartFen);
            hub.LocalBoardUpdate(lifted);
            hub.LocalBoardUpdate("rnbqkbnr/p1pppppp/8/1p6/8/8/PPPPPPPP/RNBQKBNR");

            await hub.DrainAsync().WaitAsync(TimeSpan.FromSeconds(10));

            // Half a move is not a position. Sending it puts a phantom board with a missing pawn
            // in front of the engine and blanks the panel for the duration of the drag.
            uci.Verify(m => m.LoadedEngineSetPosition(It.Is<string>(fen =>
                    fen.StartsWith(lifted, StringComparison.Ordinal))),
                Times.Never,
                "a scan taken mid-move must not reach the engine");
        }
    }
}
