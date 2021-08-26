using DgtCherub.Services;
using DynamicBoard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static DgtCherub.Helpers.ResourceLoader;

namespace DgtCherub.Controllers
{
    [Route("[controller]")]
    [Controller]
    public sealed class CherubVirtualClockController : ControllerBase
    {
        private const string RESOURCE_CLOCK_ROOT = "DgtCherub.Assets.Clocks";
        private const string RESOURCE_CLOCK_HTML = "Clock.html";

        private const string RESOURCE_CLOCK_SLIDE = $"{RESOURCE_CLOCK_ROOT}.SlideClock.{RESOURCE_CLOCK_HTML}";
        private const string RESOURCE_CLOCK_TEST = $"{RESOURCE_CLOCK_ROOT}.TestClock.{RESOURCE_CLOCK_HTML}";

        private const string RESOURCE_CLOCK_INDEX = $"{RESOURCE_CLOCK_ROOT}.index.html";
        private const string RESOURCE_CLOCK_FAV = $"{RESOURCE_CLOCK_ROOT}.favicon.png";
        private const string RESOURCE_CLOCK_SVG = $"{RESOURCE_CLOCK_ROOT}.DgtAngelLogo.svg";

        private const string MIME_HTM = "text/html";
        private const string MIME_PNG = "image/png";
        private const string MIME_SVG = "image/svg+xml";
        private const string MIME_EVENT = "text/event-stream";

        private const string BOARD_EMPTY_FEN = "8/8/8/8/8/8/8/8";

        private const int BOARD_IMAGE_SIZE = 1024;

        private readonly DateTime unixDateTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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

            IndexPageHtml = LoadResourceString(RESOURCE_CLOCK_INDEX);
            SlideClockHtml = LoadResourceString(RESOURCE_CLOCK_SLIDE);
            TestClockHtml = LoadResourceString(RESOURCE_CLOCK_TEST);

            FavIcon = LoadResource(RESOURCE_CLOCK_FAV);
            SvgLogo = LoadResource(RESOURCE_CLOCK_SVG);
        }

        [HttpGet]
        [Route("/")]
        public ContentResult GetIndex()
        {
            // http://localhost:37964/
            _logger?.LogTrace("Clock index requested");

            return new ContentResult
            {
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
                Content = IndexPageHtml,
            };
        }

        [HttpGet]
        [Route("{action}/{clock}")]
        public ContentResult GetClock(string clock)
        {
            // http://localhost:37964/CherubVirtualClock/GetClock
            _logger?.LogTrace($"Clock requested {clock}");

            try
            {
                return new ContentResult
                {
                    ContentType = MIME_HTM,
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
            _logger?.LogTrace($"Board image requested {board}");

            string local = _appDataService.IsLocalBoardAvailable ? _appDataService.LocalBoardFEN : _appDataService.ChessDotComBoardFEN;
            string remote = _appDataService.IsRemoteBoardAvailable ? _appDataService.ChessDotComBoardFEN : _appDataService.LocalBoardFEN;

            using Bitmap bmpOut = board.ToLowerInvariant() switch
            {
                "local" => _appDataService.IsLocalBoardAvailable ? await _boardRenderer.GetImageDiffFromFenAsync(local, remote, BOARD_IMAGE_SIZE, _appDataService.IsWhiteOnBottom)
                                                                 : await _boardRenderer.GetImageDiffFromFenAsync(BOARD_EMPTY_FEN, BOARD_EMPTY_FEN, BOARD_IMAGE_SIZE, _appDataService.IsWhiteOnBottom),
                "remote" => _appDataService.IsRemoteBoardAvailable ? await _boardRenderer.GetImageDiffFromFenAsync(remote, local, BOARD_IMAGE_SIZE, _appDataService.IsWhiteOnBottom)
                                                                   : await _boardRenderer.GetImageDiffFromFenAsync(BOARD_EMPTY_FEN, BOARD_EMPTY_FEN, BOARD_IMAGE_SIZE, _appDataService.IsWhiteOnBottom),
                _ => await _boardRenderer.GetImageDiffFromFenAsync(BOARD_EMPTY_FEN, BOARD_EMPTY_FEN, BOARD_IMAGE_SIZE, _appDataService.IsWhiteOnBottom)
            };

            return File(((byte[])(new ImageConverter()).ConvertTo(bmpOut, typeof(byte[]))), MIME_PNG);
        }

        [HttpGet]
        [Route("{action}/{fileName}")]
        public ActionResult Images(string fileName)
        {
            _logger?.LogTrace($"Image requested {fileName}");

            try
            {
                return File(fileName.ToLowerInvariant() switch
                {
                    "dgtangellogo.svg" => SvgLogo,
                    "favicon.png" => FavIcon,
                    _ => throw new FileNotFoundException()
                },
                    fileName.EndsWith(".svg") ? MIME_SVG : MIME_PNG);
            }
            catch (FileNotFoundException)
            {
                return StatusCode(404);
            }
        }

        [HttpGet]
        [Route("{action}/{clientUtcMs}")]
        public async Task GetStuff(string clientUtcMs)
        {
            // http://localhost:37964/CherubVirtualClock/GetStuff
            _logger?.LogTrace($"Clock client connected running {clientUtcMs}");

            //TODO: No initial local board without piece move
            //TODO: Remote board does not clear on disconnect



            int clientServerTimeDiff = (int)(double.Parse(clientUtcMs) - DateTime.Now.ToUniversalTime().Subtract(unixDateTime).TotalMilliseconds);

            Response.Headers.Add("Content-Type", MIME_EVENT);

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
