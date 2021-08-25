using DgtCherub.Services;
using DynamicBoard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DgtCherub.Controllers
{
    [Route("[controller]")]
    [Controller]
    public sealed class CherubVirtualClockController : ControllerBase
    {
        private const string RESOURCE_CLOCK_ROOT = "DgtCherub.Assets.Clocks";
        private const string RESOURCE_CLOCK_INDEX = "index.html";
        private const string RESOURCE_CLOCK_SLIDE_NAME = "SlideClock";
        private const string RESOURCE_CLOCK_TEST_NAME = "TestClock";
        private const string RESOURCE_CLOCK_HTML = "Clock.html";
        private const string RESOURCE_CLOCK_FAV = "favicon.png";
        private const string RESOURCE_CLOCK_SVG = "DgtAngelLogo.svg";

        private readonly ILogger _logger;
        private readonly IAngelHubService _appDataService;
        private readonly IBoardRenderer _boardRenderer;

        private readonly string IndexPageHtml;
        private readonly string TestClockHtml;
        private readonly string SlideClockHtml;
        private readonly byte[] FavIcon;
        private readonly byte[] SvgLogo;

        public CherubVirtualClockController(ILogger<CherubVirtualClockController> logger, IAngelHubService appData, IBoardRenderer boardRenderer)
        {
            _logger = logger;
            _appDataService = appData;
            _boardRenderer = boardRenderer;

            using System.IO.Stream slideClockStream = Assembly.GetExecutingAssembly()
                                                             .GetManifestResourceStream($"{RESOURCE_CLOCK_ROOT}.{RESOURCE_CLOCK_SLIDE_NAME}.{RESOURCE_CLOCK_HTML}");
            
            using StreamReader slideReader = new(slideClockStream);
            SlideClockHtml = slideReader.ReadToEnd();

            using System.IO.Stream testClockStream = Assembly.GetExecutingAssembly()
                                                 .GetManifestResourceStream($"{RESOURCE_CLOCK_ROOT}.{RESOURCE_CLOCK_TEST_NAME}.{RESOURCE_CLOCK_HTML}");

            using StreamReader testReader = new(testClockStream);
            TestClockHtml = testReader.ReadToEnd();

            using System.IO.Stream indexHtmlStream = Assembly.GetExecutingAssembly()
                                                             .GetManifestResourceStream($"{RESOURCE_CLOCK_ROOT}.{RESOURCE_CLOCK_INDEX}");

            using StreamReader reader = new(indexHtmlStream);
            IndexPageHtml = reader.ReadToEnd();



            using System.IO.Stream favIconStream = Assembly.GetExecutingAssembly()
                                                           .GetManifestResourceStream($"{RESOURCE_CLOCK_ROOT}.{RESOURCE_CLOCK_FAV}");

            using MemoryStream memoryStream = new();
            favIconStream.CopyTo(memoryStream);
            FavIcon = memoryStream.ToArray();

            using System.IO.Stream svgIconStream = Assembly.GetExecutingAssembly()
                                                           .GetManifestResourceStream($"{RESOURCE_CLOCK_ROOT}.{RESOURCE_CLOCK_SVG}");

            using MemoryStream memoryStreamSvg = new();
            svgIconStream.CopyTo(memoryStreamSvg);
            SvgLogo = memoryStreamSvg.ToArray();
        }

        [HttpGet]
        [Route("/")]
        public ContentResult GetIndex()
        {
            // http://localhost:37964/CherubVirtualClock/GetClock

            // Uncomment to load from disk (dev only)
            //string htmlOut = System.IO.File.ReadAllText(@"C:/TESTHTML2/index.html");
            string htmlOut = IndexPageHtml;

            return new ContentResult
            {
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
                Content = htmlOut,
            };
        }

        [HttpGet]
        [Route("{action}/{clock}")]
        public ContentResult GetClock(string clock)
        {
            // http://localhost:37964/CherubVirtualClock/GetClock

            // Uncomment to load from disk (dev only)
            string htmlOut = System.IO.File.ReadAllText(@"C:/TESTHTML2/Clock.html");
            //string htmlOut = IndexPageHtml;

            try
            {
                return new ContentResult
                {
                    ContentType = "text/html",
                    StatusCode = (int)HttpStatusCode.OK,
                    Content = clock switch
                    {
                        "GreySlide" => SlideClockHtml,
                        "TestClock" => TestClockHtml,
                        _ => throw new FileNotFoundException()
                    },
                };
            }
            catch (FileNotFoundException)
            {
                return new ContentResult { StatusCode = (int)HttpStatusCode.NotFound };
            }
        }

        [HttpGet]
        [Route("{action}/{board}")]
        public async Task<FileContentResult> BoardImage(string board)
        {
            ImageConverter converter = new();

            string local = _appDataService.IsLocalBoardAvailable ? _appDataService.LocalBoardFEN : _appDataService.ChessDotComBoardFEN;
            string remote = _appDataService.IsRemoteBoardAvailable ? _appDataService.ChessDotComBoardFEN : _appDataService.LocalBoardFEN;

            switch (board.ToLowerInvariant())
            {
                case "local":
                    Bitmap localBmpOut = _appDataService.IsLocalBoardAvailable ? await _boardRenderer.GetImageDiffFromFenAsync(local, remote, 1024, _appDataService.IsWhiteOnBottom)
                                                                               : await _boardRenderer.GetImageDiffFromFenAsync("8/8/8/8/8/8/8/8", "8/8/8/8/8/8/8/8", 2048, _appDataService.IsWhiteOnBottom);

                    return File(((byte[])converter.ConvertTo(localBmpOut, typeof(byte[]))), "image/png");
                case "remote":
                    Bitmap remBmpOut = _appDataService.IsRemoteBoardAvailable ? await _boardRenderer.GetImageDiffFromFenAsync(remote, local, 1024, _appDataService.IsWhiteOnBottom)
                                                                              : await _boardRenderer.GetImageDiffFromFenAsync("8/8/8/8/8/8/8/8", "8/8/8/8/8/8/8/8", 2048, _appDataService.IsWhiteOnBottom);

                    return File(((byte[])converter.ConvertTo(remBmpOut, typeof(byte[]))), "image/png");
                default:
                    Bitmap blankBmpOut = await _boardRenderer.GetImageDiffFromFenAsync("8/8/8/8/8/8/8/8", "8/8/8/8/8/8/8/8", 2048, _appDataService.IsWhiteOnBottom);

                    return File(((byte[])converter.ConvertTo(blankBmpOut, typeof(byte[]))), "image/png");
            }
        }

        [HttpGet]
        [Route("{action}/{fileName}")]
        public ActionResult Images(string fileName)
        {
            if (SvgLogo != null && !string.IsNullOrWhiteSpace(fileName) && fileName == "DgtAngelLogo.svg")
            {
                byte[] imageData = SvgLogo;
                string fileType = "image/svg+xml";
                return File(imageData, fileType);
            }
            else if (FavIcon != null && !string.IsNullOrWhiteSpace(fileName) && (fileName == "favicon.png"))
            {
                byte[] imageData = FavIcon;
                string fileType = "image/png";
                return File(imageData, fileType);
            }
            else if (!string.IsNullOrWhiteSpace(fileName) && (fileName == "blah" || fileName == "blah.jpg"))
            {
                byte[] imageData = System.IO.File.ReadAllBytes(@"C:/TESTHTML/test.jpg");
                string fileType = "image/jpeg";
                return File(imageData, fileType);
            }

            return StatusCode(404);
        }

        [HttpGet]
        [Route("{action}/{clientUtcMs}")]
        public async Task GetStuff(string clientUtcMs)
        {
            //TODO: No initial local board without piece move
            //TODO: Remote board does not clear on disconnect
            //TODO: Clock runs with no active game

            // http://localhost:37964/CherubVirtualClock/GetStuff

            int clientServerTimeDiff = (int)(double.Parse(clientUtcMs) - DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds);

            Response.Headers.Add("Content-Type", "text/event-stream");

            _appDataService.OnBoardMissmatch += async () =>
            {
                string jsonString = JsonSerializer.Serialize(new
                {
                    MessageType = "OnBoardMissmatch",
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                });

                string dataItem = $"data: {jsonString}{Environment.NewLine}{Environment.NewLine}";
                byte[] dataItemBytes = ASCIIEncoding.ASCII.GetBytes(dataItem);
                await Response.Body.WriteAsync(dataItemBytes);
                await Response.Body.FlushAsync();
            };

            _appDataService.OnBoardMatch += async () =>
            {
                string jsonString = JsonSerializer.Serialize(new
                {
                    MessageType = "OnBoardMatch",
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                });

                string dataItem = $"data: {jsonString}{Environment.NewLine}{Environment.NewLine}";
                byte[] dataItemBytes = ASCIIEncoding.ASCII.GetBytes(dataItem);
                await Response.Body.WriteAsync(dataItemBytes);
                await Response.Body.FlushAsync();
            };

            //Send on connect
            if (_appDataService.LocalBoardFEN != "")
            {

                string jsonString = JsonSerializer.Serialize(new
                {
                    MessageType = "OnLocalFenChange",
                    BoardFen = _appDataService.LocalBoardFEN,
                    _appDataService.IsWhiteOnBottom,
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                });

                string dataItem = $"data: {jsonString}{Environment.NewLine}{Environment.NewLine}";
                byte[] dataItemBytes = ASCIIEncoding.ASCII.GetBytes(dataItem);
                await Response.Body.WriteAsync(dataItemBytes);
                await Response.Body.FlushAsync();
            }

            if (_appDataService.ChessDotComBoardFEN != "")
            {
                string jsonString = JsonSerializer.Serialize(new
                {
                    MessageType = "OnRemoteFenChange",
                    BoardFen = _appDataService.ChessDotComBoardFEN,
                    _appDataService.IsWhiteOnBottom,
                    _appDataService.LastMove,
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                });

                string dataItem = $"data: {jsonString}{Environment.NewLine}{Environment.NewLine}";
                byte[] dataItemBytes = ASCIIEncoding.ASCII.GetBytes(dataItem);
                await Response.Body.WriteAsync(dataItemBytes);
                await Response.Body.FlushAsync();
            }

            _appDataService.OnOrientationFlipped += async () =>
            {
                string jsonString = JsonSerializer.Serialize(new
                {
                    MessageType = "OnOrientationFlipped",
                    LocalBoardFen = _appDataService.LocalBoardFEN,
                    RemoteBoardFen = _appDataService.ChessDotComBoardFEN,
                    _appDataService.IsWhiteOnBottom,
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                });

                string dataItem = $"data: {jsonString}{Environment.NewLine}{Environment.NewLine}";
                byte[] dataItemBytes = ASCIIEncoding.ASCII.GetBytes(dataItem);
                await Response.Body.WriteAsync(dataItemBytes);
                await Response.Body.FlushAsync();
            };


            _appDataService.OnClockChange += async () =>
            {
                string jsonString = JsonSerializer.Serialize(new
                {
                    MessageType = "OnClockChange",
                    WhiteClockMsRemaining = _appDataService.WhiteClockMs,
                    BlackClockMsRemaining = _appDataService.BlackClockMs,
                    IsGameActive = _appDataService.RunWhoString != "3",
                    IsWhiteToPlay = _appDataService.RunWhoString == "1",
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                });

                string dataItem = $"data: {jsonString}{Environment.NewLine}{Environment.NewLine}";
                byte[] dataItemBytes = ASCIIEncoding.ASCII.GetBytes(dataItem);
                await Response.Body.WriteAsync(dataItemBytes);
                await Response.Body.FlushAsync();
            };

            _appDataService.OnLocalFenChange += async () =>
            {
                string jsonString = JsonSerializer.Serialize(new
                {
                    MessageType = "OnLocalFenChange",
                    BoardFen = _appDataService.LocalBoardFEN,
                    _appDataService.IsWhiteOnBottom,
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                });

                string dataItem = $"data: {jsonString}{Environment.NewLine}{Environment.NewLine}";
                byte[] dataItemBytes = ASCIIEncoding.ASCII.GetBytes(dataItem);
                await Response.Body.WriteAsync(dataItemBytes);
                await Response.Body.FlushAsync();
            };

            _appDataService.OnRemoteFenChange += async () =>
            {
                string jsonString = JsonSerializer.Serialize(new
                {
                    MessageType = "OnRemoteFenChange",
                    BoardFen = _appDataService.ChessDotComBoardFEN,
                    _appDataService.IsWhiteOnBottom,
                    _appDataService.LastMove,
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                });

                string dataItem = $"data: {jsonString}{Environment.NewLine}{Environment.NewLine}";
                await Response.Body.WriteAsync(ASCIIEncoding.ASCII.GetBytes(dataItem));
                await Response.Body.FlushAsync();
            };

            _appDataService.OnRemoteDisconnect += async () =>
            {
                string jsonString = JsonSerializer.Serialize(new
                {
                    MessageType = "OnRemoteStopWatch",
                    BoardFen = "",
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                });

                string dataItem = $"data: {jsonString}{Environment.NewLine}{Environment.NewLine}";
                await Response.Body.WriteAsync(ASCIIEncoding.ASCII.GetBytes(dataItem));
                await Response.Body.FlushAsync();
            };

            //Keep Alive
            while (true)
            {
                string jsonString = JsonSerializer.Serialize(new
                {
                    MessageType = "Keep-Alive",
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                });

                string dataItem = $"data: {jsonString}{Environment.NewLine}{Environment.NewLine}";
                byte[] dataItemBytes = ASCIIEncoding.ASCII.GetBytes(dataItem);
                await Response.Body.WriteAsync(dataItemBytes);
                await Response.Body.FlushAsync();

                await Task.Delay(TimeSpan.FromSeconds(60 * 5));
            }
        }
    }
}
