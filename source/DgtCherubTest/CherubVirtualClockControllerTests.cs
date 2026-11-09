using DgtCherub.Controllers;
using DgtCherub.Services;
using DynamicBoard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UciComms;

namespace DgtCherubTest
{
    /// <summary>
    /// The virtual clock is served to a phone browser over plain HTTP and is the only part
    /// of this product a player looks at while playing. Its endpoints must answer, answer
    /// with the right content type, and - crucially - 404 rather than fall over when asked
    /// for something that is not a recognised clock or asset.
    /// </summary>
    [TestClass]
    public class CherubVirtualClockControllerTests
    {
        private const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";
        private const string AfterE4Fen = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR";

        private static (CherubVirtualClockController controller, FakeBoardRenderer renderer, Mock<IAngelHubService> hub) Create(
            bool localAvailable = true,
            bool remoteAvailable = true)
        {
            Mock<IAngelHubService> hub = new(MockBehavior.Loose) { DefaultValue = DefaultValue.Empty };
            hub.SetupGet(h => h.IsWhiteOnBottom).Returns(true);
            hub.SetupGet(h => h.IsLocalBoardAvailable).Returns(localAvailable);
            hub.SetupGet(h => h.IsRemoteBoardAvailable).Returns(remoteAvailable);
            hub.SetupGet(h => h.LocalBoardFEN).Returns(StartFen);
            hub.SetupGet(h => h.RemoteBoardFEN).Returns(AfterE4Fen);

            FakeBoardRenderer renderer = new();

            CherubVirtualClockController controller = new(
                NullLogger<CherubVirtualClockController>.Instance,
                hub.Object,
                renderer,
                new Mock<IUciEngineManager>(MockBehavior.Loose).Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            };

            return (controller, renderer, hub);
        }

        [TestMethod(DisplayName = "The index page is served as HTML")]
        public void IndexIsServed()
        {
            (CherubVirtualClockController controller, _, _) = Create();

            ContentResult result = controller.GetIndex();

            Assert.AreEqual(200, result.StatusCode);
            Assert.AreEqual("text/html", result.ContentType);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Content));
        }

        [TestMethod(DisplayName = "Every offered clock is served as HTML")]
        [DataRow("GreySlide")]
        [DataRow("GreyFixed")]
        [DataRow("LocalBroadcast")]
        [DataRow("RemoteBroadcast")]
        [DataRow("WingedHorse")]
        public void KnownClocksAreServed(string clock)
        {
            (CherubVirtualClockController controller, _, _) = Create();

            ContentResult result = controller.GetClock(clock);

            Assert.AreEqual(200, result.StatusCode, $"'{clock}' should be a servable clock");
            Assert.AreEqual("text/html", result.ContentType);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Content));
        }

        [TestMethod(DisplayName = "An unknown clock is a 404, not an error")]
        public void UnknownClockIsNotFound()
        {
            (CherubVirtualClockController controller, _, _) = Create();

            ContentResult result = controller.GetClock("SomethingElse");

            Assert.AreEqual(404, result.StatusCode);
        }

        [TestMethod(DisplayName = "A clock's template assets are served with the right content type")]
        [DataRow("WingedHorse", "TemplateData", "style.css", "text/css")]
        [DataRow("WingedHorse", "TemplateData", "Keyboard.svg", "image/svg+xml")]
        [DataRow("WingedHorse", "Build", "Build.loader.js", "text/javascript")]
        public void ClockAssetsAreServed(string clock, string directory, string fileName, string expectedMime)
        {
            (CherubVirtualClockController controller, _, _) = Create();

            FileContentResult? result = controller.GetFile(clock, directory, fileName) as FileContentResult;

            Assert.IsNotNull(result, $"{clock}/{directory}/{fileName} should resolve");
            Assert.AreEqual(expectedMime, result.ContentType);
            Assert.IsTrue(result.FileContents.Length > 0);
        }

        [TestMethod(DisplayName = "An unknown asset is a 404")]
        [DataRow("WingedHorse", "TemplateData", "nope.png")]
        [DataRow("WingedHorse", "TemplateData", "style.txt")]
        [DataRow("NotAClock", "TemplateData", "style.css")]
        public void UnknownAssetIsNotFound(string clock, string directory, string fileName)
        {
            (CherubVirtualClockController controller, _, _) = Create();

            StatusCodeResult? result = controller.GetFile(clock, directory, fileName) as StatusCodeResult;

            Assert.IsNotNull(result, "an unknown asset must return a status result, not content");
            Assert.AreEqual(404, result.StatusCode);
        }

        [TestMethod(DisplayName = "The logo and favicon are served")]
        [DataRow("favicon.png")]
        [DataRow("DgtAngelLogo.svg")]
        public void ImagesAreServed(string fileName)
        {
            (CherubVirtualClockController controller, _, _) = Create();

            FileContentResult? result = controller.Images(fileName) as FileContentResult;

            Assert.IsNotNull(result);
            Assert.IsTrue(result.FileContents.Length > 0);
        }

        [TestMethod(DisplayName = "An unknown image is a 404")]
        public void UnknownImageIsNotFound()
        {
            (CherubVirtualClockController controller, _, _) = Create();

            StatusCodeResult? result = controller.Images("../../web.config") as StatusCodeResult;

            Assert.IsNotNull(result, "a path-looking name must not resolve to a file");
            Assert.AreEqual(404, result.StatusCode);
        }

        [TestMethod(DisplayName = "The local board image uses the local position")]
        public async Task LocalBoardImageUsesLocalPosition()
        {
            (CherubVirtualClockController controller, FakeBoardRenderer renderer, _) = Create();

            _ = await controller.BoardImage("local");

            Assert.AreEqual(1, renderer.DiffCalls.Count);
            Assert.AreEqual(StartFen, renderer.DiffCalls[0].fen);
            Assert.AreEqual(AfterE4Fen, renderer.DiffCalls[0].compare);
        }

        [TestMethod(DisplayName = "The remote board image uses the remote position")]
        public async Task RemoteBoardImageUsesRemotePosition()
        {
            (CherubVirtualClockController controller, FakeBoardRenderer renderer, _) = Create();

            _ = await controller.BoardImage("remote");

            Assert.AreEqual(1, renderer.DiffCalls.Count);
            Assert.AreEqual(AfterE4Fen, renderer.DiffCalls[0].fen);
            Assert.AreEqual(StartFen, renderer.DiffCalls[0].compare);
        }

        [TestMethod(DisplayName = "An unrecognised board name still returns an image")]
        public async Task UnknownBoardNameReturnsEmptyBoard()
        {
            (CherubVirtualClockController controller, FakeBoardRenderer renderer, _) = Create();

            FileContentResult result = await controller.BoardImage("nonsense");

            Assert.AreEqual("8/8/8/8/8/8/8/8", renderer.DiffCalls[0].fen, "an unknown board must render the empty board, not throw");
            Assert.IsTrue(result.FileContents.Length > 0);
        }

        [TestMethod(DisplayName = "With no local board the empty board is rendered")]
        public async Task MissingLocalBoardRendersEmpty()
        {
            (CherubVirtualClockController controller, FakeBoardRenderer renderer, _) = Create(localAvailable: false);

            _ = await controller.BoardImage("local");

            Assert.AreEqual("8/8/8/8/8/8/8/8", renderer.DiffCalls[0].fen);
        }

        /// <summary>
        /// The clock client passes its own clock reading as a query string. Anything the
        /// user or a stale bookmark can put in a URL must not reach a numeric parse
        /// unguarded - an unhandled FormatException becomes a 500.
        /// </summary>
        [TestMethod(DisplayName = "A non-numeric client clock reading is rejected, not thrown")]
        public async Task NonNumericClientTimeIsRejected()
        {
            (CherubVirtualClockController controller, _, _) = Create();

            await controller.GetStuff("not-a-number").WaitAsync(TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// The clock stream is server-sent events and must say so. It is set on a response
        /// that may already carry a content type, so setting it must overwrite rather than
        /// throw - which is what adding the header did.
        /// </summary>
        [TestMethod(DisplayName = "The clock stream is served as server-sent events")]
        public async Task ClockStreamIsServerSentEvents()
        {
            (CherubVirtualClockController controller, _, _) = Create();

            DefaultHttpContext context = new();
            using CancellationTokenSource clientGone = new();
            context.RequestAborted = clientGone.Token;
            context.Response.ContentType = "text/plain";
            controller.ControllerContext = new ControllerContext { HttpContext = context };

            Task streaming = controller.GetStuff("1762700000000");

            Assert.AreEqual(
                "text/event-stream",
                context.Response.ContentType,
                "the stream must be served as server-sent events, even over a content type that was already set");

            clientGone.Cancel();
            await streaming.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.IsTrue(streaming.IsCompleted, "the stream must end once the client has gone");
        }
    }

    internal sealed class FakeBoardRenderer : IBoardRenderer
    {
        public List<(string fen, string compare, int size, bool white)> DiffCalls { get; } = new();

        public byte[] Png { get; set; } = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        public Task<byte[]> GetPngImageFromFenAsync(in string fenString, in int imageSize, in bool isFromWhitesPerspective)
            => Task.FromResult(Png);

        public Task<byte[]> GetPngImageDiffFromFenAsync(in string fenString, in string compFenString, in int imageSize, in bool isFromWhitesPerspective)
        {
            DiffCalls.Add((fenString, compFenString, imageSize, isFromWhitesPerspective));
            return Task.FromResult(Png);
        }
    }
}
