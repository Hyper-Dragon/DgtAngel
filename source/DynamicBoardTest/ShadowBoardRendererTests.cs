using DynamicBoard;
using DynamicBoardTest.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicBoardTest
{
    /// <summary>
    /// Behaviour specific to the locally drawn renderer. The contract tests cover what
    /// every renderer must do; these cover its own image cache, which is shared across
    /// instances and therefore a place where one call can contaminate another.
    /// </summary>
    [TestClass]
    public class ShadowBoardRendererTests
    {
        [TestMethod(DisplayName = "The same position renders identically on a repeat call")]
        public async Task RepeatRenderIsIdentical()
        {
            IBoardRenderer renderer = RendererFactory.CreateShadowRenderer();
            const string position = "rnbqk1nr/ppp2pbp/4p1p1/3p4/3P1B2/2PBPN2/PP3PPP/RN1QK2R";

            byte[] first = await renderer.GetPngImageFromFenAsync(position, 160, true);
            byte[] second = await renderer.GetPngImageFromFenAsync(position, 160, true);

            CollectionAssert.AreEqual(first, second);
        }

        [TestMethod(DisplayName = "Each perspective gets its own image for the same position")]
        public async Task PerspectiveIsNotSharedThroughTheCache()
        {
            IBoardRenderer renderer = RendererFactory.CreateShadowRenderer();
            const string position = "rnbqk1nr/ppp2pbp/4p1p1/3p4/3P1B2/2PBPN2/PP3PPP/RN1QK2R";

            byte[] whiteBottom = await renderer.GetPngImageFromFenAsync(position, 160, true);
            byte[] blackBottom = await renderer.GetPngImageFromFenAsync(position, 160, false);

            CollectionAssert.AreNotEqual(whiteBottom, blackBottom, "a second perspective must not be served the first one's image");
        }

        [TestMethod(DisplayName = "Requested sizes are not shared through the cache")]
        public async Task SizeIsNotSharedThroughTheCache()
        {
            IBoardRenderer renderer = RendererFactory.CreateShadowRenderer();
            const string position = "rnbqk1nr/ppp2pbp/4p1p1/3p4/3P1B2/2PBPN2/PP3PPP/RN1QK2R";

            byte[] small = await renderer.GetPngImageFromFenAsync(position, 96, true);
            byte[] large = await renderer.GetPngImageFromFenAsync(position, 320, true);

            using System.Drawing.Bitmap smallBitmap = BoardRendererContractTests.Decode(small);
            using System.Drawing.Bitmap largeBitmap = BoardRendererContractTests.Decode(large);

            Assert.AreEqual(96, smallBitmap.Width);
            Assert.AreEqual(320, largeBitmap.Width);
        }

        [TestMethod(DisplayName = "Many distinct positions all render")]
        public async Task ManyDistinctPositionsRender()
        {
            IBoardRenderer renderer = RendererFactory.CreateShadowRenderer();

            HashSet<string> fingerprints = new();

            for (int seed = 1; seed <= 40; seed++)
            {
                string boardFen = FenBoard.Format(FenBoard.RandomBoard(seed, 20));

                byte[] png = await renderer.GetPngImageFromFenAsync(boardFen, 100, true);

                Assert.IsTrue(png.Length > 8, $"seed {seed} produced no image");
                _ = fingerprints.Add(System.Convert.ToBase64String(png));
            }

            Assert.IsTrue(fingerprints.Count > 30, $"40 distinct positions produced only {fingerprints.Count} distinct images");
        }
    }
}
