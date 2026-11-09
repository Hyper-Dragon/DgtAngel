using DynamicBoard;
using DynamicBoardTest.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace DynamicBoardTest
{
    /// <summary>
    /// The behaviour every <see cref="IBoardRenderer"/> must have. Expectations are written
    /// from what a renderer is for - produce a PNG of the requested size for a position - not
    /// from what the implementation happens to do.
    /// </summary>
    [TestClass]
    public abstract class BoardRendererContractTests
    {
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        protected abstract IBoardRenderer CreateRenderer();

        [TestMethod(DisplayName = "Produces a PNG for a normal position")]
        public async Task ProducesPng()
        {
            IBoardRenderer renderer = CreateRenderer();

            byte[] png = await renderer.GetPngImageFromFenAsync(FenBoard.Start, 200, true);

            Assert.IsNotNull(png);
            CollectionAssert.AreEqual(PngSignature, png[..8], "output should carry a PNG signature");
        }

        [TestMethod(DisplayName = "Renders at exactly the requested size")]
        [DataRow(64)]
        [DataRow(200)]
        [DataRow(480)]
        public async Task RendersAtRequestedSize(int size)
        {
            IBoardRenderer renderer = CreateRenderer();

            byte[] png = await renderer.GetPngImageFromFenAsync(FenBoard.Start, size, true);

            using Bitmap bmp = Decode(png);
            Assert.AreEqual(size, bmp.Width);
            Assert.AreEqual(size, bmp.Height);
        }

        [TestMethod(DisplayName = "The same position renders identically every time")]
        public async Task RendersAreDeterministic()
        {
            IBoardRenderer renderer = CreateRenderer();
            const string position = "rnbqk1nr/ppp2pbp/4p1p1/3p4/3P1B2/2PBPN2/PP3PPP/RN1QK2R";

            byte[] first = await renderer.GetPngImageFromFenAsync(position, 240, true);
            byte[] second = await renderer.GetPngImageFromFenAsync(position, 240, true);

            CollectionAssert.AreEqual(first, second, "rendering must not depend on time or ordering");
        }

        [TestMethod(DisplayName = "Perspective actually changes the image")]
        public async Task PerspectiveChangesImage()
        {
            IBoardRenderer renderer = CreateRenderer();
            const string position = "rnbqk1nr/ppp2pbp/4p1p1/3p4/3P1B2/2PBPN2/PP3PPP/RN1QK2R";

            byte[] whiteBottom = await renderer.GetPngImageFromFenAsync(position, 200, true);
            byte[] blackBottom = await renderer.GetPngImageFromFenAsync(position, 200, false);

            CollectionAssert.AreNotEqual(whiteBottom, blackBottom, "flipping the board must change the image");
        }

        [TestMethod(DisplayName = "An empty board still renders")]
        public async Task EmptyBoardRenders()
        {
            IBoardRenderer renderer = CreateRenderer();

            byte[] png = await renderer.GetPngImageFromFenAsync(FenBoard.Empty, 160, true);

            CollectionAssert.AreEqual(PngSignature, png[..8]);
        }

        [TestMethod(DisplayName = "A junk position returns an image rather than throwing")]
        [DataRow("thisfenisjunk!!")]
        [DataRow("")]
        [DataRow("8/8/8")]
        public async Task JunkFenReturnsAnImage(string fen)
        {
            IBoardRenderer renderer = CreateRenderer();

            byte[] png = await renderer.GetPngImageFromFenAsync(fen, 120, false);

            Assert.IsNotNull(png);
            CollectionAssert.AreEqual(PngSignature, png[..8], $"'{fen}' should still produce a placeholder image");
        }

        [TestMethod(DisplayName = "Concurrent renders all produce valid images")]
        public async Task ConcurrentRendersAreSafe()
        {
            IBoardRenderer renderer = CreateRenderer();
            const string position = "rnbqk1nr/ppp2pbp/4p1p1/3p4/3P1B2/2PBPN2/PP3PPP/RN1QK2R";

            List<Task<byte[]>> renders = new();
            for (int i = 0; i < 24; i++)
            {
                bool whiteBottom = i % 2 == 0;
                renders.Add(Task.Run(() => renderer.GetPngImageFromFenAsync(position, 120, whiteBottom)));
            }

            byte[][] results = await Task.WhenAll(renders);

            foreach (byte[] png in results)
            {
                CollectionAssert.AreEqual(PngSignature, png[..8], "every concurrent render must be a complete PNG");
            }
        }

        [TestMethod(DisplayName = "An unchanged position diffs to the same image")]
        public async Task DiffOfIdenticalPositionsMatchesPlainRender()
        {
            IBoardRenderer renderer = CreateRenderer();
            const string position = "rnbqk1nr/ppp2pbp/4p1p1/3p4/3P1B2/2PBPN2/PP3PPP/RN1QK2R";

            byte[] plain = await renderer.GetPngImageFromFenAsync(position, 200, true);
            byte[] diff = await renderer.GetPngImageDiffFromFenAsync(position, position, 200, true);

            CollectionAssert.AreEqual(plain, diff, "nothing changed, so the diff must look like the plain board");
        }

        [TestMethod(DisplayName = "A changed position is highlighted")]
        public async Task DiffOfDifferentPositionsIsMarked()
        {
            IBoardRenderer renderer = CreateRenderer();
            const string before = FenBoard.Start;
            string after = FenBoard.Move(before, "e2", "e4");

            byte[] plain = await renderer.GetPngImageFromFenAsync(after, 200, true);
            byte[] diff = await renderer.GetPngImageDiffFromFenAsync(after, before, 200, true);

            CollectionAssert.AreNotEqual(plain, diff, "the changed squares must be visible");
        }

        internal static Bitmap Decode(byte[] png)
        {
            MemoryStream stream = new(png);
            return new Bitmap(stream);
        }
    }

    [TestClass]
    public sealed class ShadowBoardRendererContractTests : BoardRendererContractTests
    {
        protected override IBoardRenderer CreateRenderer() => RendererFactory.CreateShadowRenderer();
    }
}
