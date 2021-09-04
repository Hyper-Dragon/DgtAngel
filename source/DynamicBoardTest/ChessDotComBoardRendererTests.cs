using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicBoard.Tests
{
    [TestClass()]
    public class ChessDotComBoardRendererTests
    {
        private static IServiceProvider? provider = null;


        [ClassInitialize]
        public static void TestFixtureSetup(TestContext context)
        {
            ServiceCollection services = new();

            services.AddLogging(logging => logging.AddConsole())
                    .AddHttpClient()
                    .AddSingleton(typeof(IBoardRenderer), typeof(ChessDotComBoardRenderer));


            provider = services.BuildServiceProvider();
        }

        [TestInitialize]
        public void Setup()
        {
        }

        [TestMethod("Constructor DI test")]
        public void ChessDotComBoardRendererTest()
        {
            Assert.IsNotNull(provider);
            IBoardRenderer? boardRenderer = (IBoardRenderer?)provider.GetService(typeof(IBoardRenderer));
            Assert.IsNotNull(boardRenderer);
            Assert.IsInstanceOfType(boardRenderer, typeof(ChessDotComBoardRenderer));
        }

        [TestMethod("Produce image from FEN string")]
        public async Task GetPngImageFromFenAsyncTest()
        {
            Assert.IsNotNull(provider);
            IBoardRenderer? boardRenderer = (IBoardRenderer?)provider.GetService(typeof(IBoardRenderer));
            Assert.IsNotNull(boardRenderer);

            byte[] pngOut = await boardRenderer.GetPngImageFromFenAsync("rnbqk1nr/ppp2pbp/4p1p1/3p4/3P1B2/2PBPN2/PP3PPP/RN1QK2R", 600, true);

            Assert.IsNotNull(pngOut);
            Assert.IsTrue(pngOut.Length > 100);
        }

        [TestMethod("Produce the same 'diff' image from identical FEN strings")]
        public async Task GetPngImageDiffFromFenAsyncTest()
        {
            Assert.IsNotNull(provider);
            IBoardRenderer? boardRenderer = (IBoardRenderer?)provider.GetService(typeof(IBoardRenderer));
            Assert.IsNotNull(boardRenderer);

            byte[] pngOut1 = await boardRenderer.GetPngImageFromFenAsync("rnbqk1nr/ppp2pbp/4p1p1/3p4/3P1B2/2PBPN2/PP3PPP/RN1QK2R", 600, true);
            byte[] pngOut2 = await boardRenderer.GetPngImageDiffFromFenAsync("rnbqk1nr/ppp2pbp/4p1p1/3p4/3P1B2/2PBPN2/PP3PPP/RN1QK2R", "rnbqk1nr/ppp2pbp/4p1p1/3p4/3P1B2/2PBPN2/PP3PPP/RN1QK2R", 600, true);

            Assert.IsNotNull(pngOut1);
            Assert.IsNotNull(pngOut2);
            Assert.IsTrue(pngOut1.Length == pngOut2.Length);
            Assert.AreEqual(pngOut2, pngOut2);
        }

        [TestMethod("Pass junk Fen String - Returns Dummy Image")]
        public async Task JunkFenTest()
        {
            Assert.IsNotNull(provider);
            IBoardRenderer? boardRenderer = (IBoardRenderer?)provider.GetService(typeof(IBoardRenderer));
            Assert.IsNotNull(boardRenderer);

            byte[] pngOut = await boardRenderer.GetPngImageFromFenAsync("thisfenisjunk!!", 100, false);

            Assert.IsNotNull(pngOut);
            Assert.IsTrue(pngOut.Length > 100);
        }

        [TestMethod("Simul calls without error")]
        public void MultiCallTest()
        {
            Assert.IsNotNull(provider);
            IBoardRenderer? boardRenderer = (IBoardRenderer?)provider.GetService(typeof(IBoardRenderer));
            Assert.IsNotNull(boardRenderer);


            string[]? fens = new string[]  { "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR",
                                       "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR",
                                       "rnbqkbnr/pp1ppppp/8/2p5/4P3/8/PPPP1PPP/RNBQKBNR",
                                       "rnbqkbnr/pp1ppppp/8/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R",
                                       "rnbqkbnr/pppppppp/8/8/8/8/PPBPPPPP/RNBQKBNR",
                                       "8/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR",
                                       "rnbqkbnr/8/8/2p5/4P3/8/PPPP1PPP/RNBQKBNR",
                                       "rnbqkbnr/pp1ppppp/8/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R",
                                       "rnbqkbnr/pppppppp/8/8/8/8/PPPPPRPP/RNBQKBNR",
                                       "rnbqkbnr/pppppppp/8/8/8/8/PPPP1PPP/RNBQKBNR",
                                       "rnbqkbnr/pp1ppppp/8/2p5/4P3/8/PPPP1PPP/8",
                                       "rnbqkbnr/pp1ppppp/8/2p5/4P3/5N2/8/RNBQKB1R"};

            List<Task<byte[]>> taskList = new();

            foreach (bool isWhiteBottom in new bool[] { true, false })
            {
                foreach (string? fen in fens)
                {
                    taskList.Add(Task.Run(() => boardRenderer.GetPngImageFromFenAsync(fen, 1000, isWhiteBottom)));
                }
            }

            //Wait for them all to complete
            Task<byte[]>[]? taskListArray = taskList.ToArray();
            Task.WaitAll(taskListArray);

            foreach (Task<byte[]>? t in taskListArray)
            {
                Assert.IsTrue(t.IsCompletedSuccessfully);
                Assert.IsTrue(t.Result.Length > 100);
            }
        }
    }
}