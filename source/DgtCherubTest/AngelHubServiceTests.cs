using DgtAngelShared.Json;
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
    /// Sequencing of the message-processing queues.
    ///
    /// AngelHubService fans one incoming board state out across several bounded channels,
    /// each with its own processor. The order those processors publish in, and which state
    /// each one ends up holding, is the behaviour the voice, the mismatch matcher and the
    /// clock all depend on. Every test here ends with DrainAsync so it asserts on the
    /// finished result rather than on a guess about how long processing takes.
    /// </summary>
    [TestClass]
    public class AngelHubServiceTests
    {
        private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

        private const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";
        private const string AfterE4Fen = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR";

        private static AngelHubService CreateHub(out InstantTimeProvider clock)
        {
            clock = new InstantTimeProvider();
            Mock<IUciEngineManager> uci = new(MockBehavior.Loose);

            return new AngelHubService(NullLogger<AngelHubService>.Instance, uci.Object) { TimeSource = clock };
        }

        private static BoardState State(
            string fen,
            string lastFen = "",
            string lastMove = "",
            int whiteMs = 0,
            int blackMs = 0,
            TurnCode clockTurn = TurnCode.NONE,
            bool whiteOnBottom = true)
            => new()
            {
                PageUrl = "https://example.test/game",
                CaptureTimeMs = 0,
                State = new DgtAngelShared.Json.State { Code = ResponseCode.GAME_IN_PROGRESS, Message = "" },
                BoardConnection = new Boardconnection { BoardState = SiteToBoardConnectionState.ACTIVE, ConMessage = "connected" },
                Board = new Board
                {
                    IsWhiteOnBottom = whiteOnBottom,
                    ClockTurn = clockTurn,
                    FenTurn = TurnCode.WHITE,
                    LastMove = lastMove,
                    Ending = "",
                    FenString = fen,
                    LastFenString = lastFen,
                    Clocks = new Clocks
                    {
                        // Capture "now" so elapsed-time correction has nothing to remove by default.
                        CaptureTimeMs = InstantTimeProvider.DefaultStart.ToUnixTimeMilliseconds(),
                        WhiteClock = whiteMs,
                        BlackClock = blackMs,
                    },
                },
            };

        [TestMethod(DisplayName = "A remote board update is published once processing finishes")]
        public async Task RemoteBoardUpdateIsPublished()
        {
            AngelHubService hub = CreateHub(out _);

            List<(string from, string to)> published = new();
            hub.OnRemoteFenChange += (from, to, _, _, _, _, _) => published.Add((from, to));

            hub.RemoteBoardUpdated(State(StartFen));
            await hub.DrainAsync().WaitAsync(Patience);

            Assert.AreEqual(StartFen, hub.RemoteBoardFEN);
            Assert.AreEqual(1, published.Count, "one update should publish exactly one change");
            Assert.AreEqual(StartFen, published[0].to);
        }

        [TestMethod(DisplayName = "The most recent board wins when updates arrive back to back")]
        public async Task LatestBoardWins()
        {
            AngelHubService hub = CreateHub(out _);

            hub.RemoteBoardUpdated(State(StartFen));
            await hub.DrainAsync().WaitAsync(Patience);

            hub.RemoteBoardUpdated(State(AfterE4Fen, StartFen, "e4"));
            await hub.DrainAsync().WaitAsync(Patience);

            Assert.AreEqual(AfterE4Fen, hub.RemoteBoardFEN, "the hub must end up holding the newest position");
        }

        [TestMethod(DisplayName = "A local board update is published once processing finishes")]
        public async Task LocalBoardUpdateIsPublished()
        {
            AngelHubService hub = CreateHub(out _);

            List<string> published = new();
            hub.OnLocalFenChange += fen => published.Add(fen);

            hub.LocalBoardUpdate(StartFen);
            await hub.DrainAsync().WaitAsync(Patience);

            Assert.AreEqual(StartFen, hub.LocalBoardFEN);
            Assert.AreEqual(1, published.Count);
            Assert.AreEqual(StartFen, published[0]);
        }

        [TestMethod(DisplayName = "The same local position twice is published once")]
        public async Task RepeatLocalBoardIsNotRepublished()
        {
            AngelHubService hub = CreateHub(out _);

            int published = 0;
            hub.OnLocalFenChange += _ => published++;

            hub.LocalBoardUpdate(StartFen);
            await hub.DrainAsync().WaitAsync(Patience);

            hub.LocalBoardUpdate(StartFen);
            await hub.DrainAsync().WaitAsync(Patience);

            Assert.AreEqual(1, published, "an unchanged board is not a change event");
        }

        [TestMethod(DisplayName = "Clocks are formatted and announced")]
        public async Task ClocksAreFormatted()
        {
            AngelHubService hub = CreateHub(out _);

            int clockChanges = 0;
            hub.OnClockChange += () => clockChanges++;

            hub.RemoteBoardUpdated(State(StartFen, whiteMs: 300_000, blackMs: 295_000, clockTurn: TurnCode.WHITE));
            await hub.DrainAsync().WaitAsync(Patience);

            Assert.AreEqual("0:05:00", hub.WhiteClock, "300 seconds is five minutes");
            Assert.AreEqual("0:04:55", hub.BlackClock, "295 seconds is four minutes fifty-five");
            Assert.IsTrue(clockChanges > 0, "the clock change must be announced");
        }

        [TestMethod(DisplayName = "The clock is corrected for time elapsed since capture")]
        public async Task ClockAccountsForElapsedTime()
        {
            AngelHubService hub = CreateHub(out InstantTimeProvider clock);

            // The state was captured at epoch + 0 ms; 60s of real time has passed since.
            clock.Advance(TimeSpan.FromSeconds(60));

            hub.RemoteBoardUpdated(State(StartFen, whiteMs: 300_000, blackMs: 300_000, clockTurn: TurnCode.WHITE));
            await hub.DrainAsync().WaitAsync(Patience);

            Assert.AreEqual("0:04:00", hub.WhiteClock, "the running clock must lose the elapsed minute");
            Assert.AreEqual("0:05:00", hub.BlackClock, "the clock that is not running must not move");
        }

        [TestMethod(DisplayName = "Orientation is only announced when it actually flips")]
        public async Task OrientationAnnouncedOnlyOnFlip()
        {
            AngelHubService hub = CreateHub(out _);

            int flips = 0;
            hub.OnOrientationFlipped += () => flips++;

            Assert.IsTrue(hub.IsWhiteOnBottom);

            hub.RemoteBoardUpdated(State(StartFen, whiteOnBottom: true));
            await hub.DrainAsync().WaitAsync(Patience);

            Assert.AreEqual(0, flips, "no change, no announcement");

            hub.RemoteBoardUpdated(State(StartFen, whiteOnBottom: false));
            await hub.DrainAsync().WaitAsync(Patience);

            Assert.AreEqual(1, flips);
            Assert.IsFalse(hub.IsWhiteOnBottom);
        }

        [TestMethod(DisplayName = "Stopping a watch clears the remote board")]
        public async Task WatchStoppedClearsRemoteBoard()
        {
            AngelHubService hub = CreateHub(out _);

            hub.RemoteBoardUpdated(State(StartFen));
            await hub.DrainAsync().WaitAsync(Patience);
            Assert.AreEqual(StartFen, hub.RemoteBoardFEN);

            await hub.WatchStateChange(CherubApiMessage.MessageTypeCode.WATCH_STOPPED, "test");

            Assert.AreEqual("", hub.RemoteBoardFEN, "a stopped watch must not leave a stale position on screen");
        }

        [TestMethod(DisplayName = "No work is left in flight after a drain")]
        public async Task DrainLeavesNothingInFlight()
        {
            AngelHubService hub = CreateHub(out _);

            hub.RemoteBoardUpdated(State(StartFen));
            await hub.DrainAsync().WaitAsync(Patience);

            Assert.AreEqual(0, hub.InFlightCount);
        }
    }
}
