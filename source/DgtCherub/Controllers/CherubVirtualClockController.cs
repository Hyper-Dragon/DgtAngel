using DgtCherub.Services;
using DynamicBoard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;
using static DgtCherub.Helpers.ResourceLoader;

namespace DgtCherub.Controllers
{
    [Route("[controller]")]
    [Controller]
    public sealed class CherubVirtualClockController : ControllerBase
    {
        private const string RESOURCE_CLOCK_ROOT = "DgtCherub.Assets.Clocks";
        private const string RESOURCE_CLOCK_HTML = "Clock.html";
        private const string FAV_ICON = "favicon.png";
        private const string SVG_LOGO = "DgtAngelLogo.svg";

        private const string CLOCK_SLIDE = "GreySlide";
        private const string CLOCK_TEST = "TestClock";

        private const string RESOURCE_CLOCK_SLIDE = $"{RESOURCE_CLOCK_ROOT}.{CLOCK_SLIDE}.{RESOURCE_CLOCK_HTML}";
        private const string RESOURCE_CLOCK_TEST = $"{RESOURCE_CLOCK_ROOT}.{CLOCK_TEST}.{RESOURCE_CLOCK_HTML}";

        private const string RESOURCE_CLOCK_INDEX = $"{RESOURCE_CLOCK_ROOT}.index.html";
        private const string RESOURCE_CLOCK_FAV = $"{RESOURCE_CLOCK_ROOT}.{FAV_ICON}";
        private const string RESOURCE_CLOCK_SVG = $"{RESOURCE_CLOCK_ROOT}.{SVG_LOGO}";

        private const string MIME_HTM = "text/html";
        private const string MIME_PNG = "image/png";
        private const string MIME_SVG = "image/svg+xml";
        private const string MIME_EVENT = "text/event-stream";

        private const string BOARD_EMPTY_FEN = "8/8/8/8/8/8/8/8";
        private const int BOARD_IMAGE_SIZE = 1024;

        private const double KEEP_ALIVE_EVERY_SECONDS = 60;

        private readonly DateTime unixDateTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly ILogger _logger;
        private readonly IAngelHubService _angelHubService;
        private readonly IBoardRenderer _boardRenderer;

        private readonly string IndexPageHtml;
        private readonly string TestClockHtml;
        private readonly string SlideClockHtml;
        private readonly byte[] FavIcon;
        private readonly byte[] SvgLogo;

        public CherubVirtualClockController(ILogger<CherubVirtualClockController> logger, IAngelHubService appData, IBoardRenderer boardRenderer)
        {
            _logger = logger;
            _angelHubService = appData;
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
                        CLOCK_SLIDE => SlideClockHtml,
                        CLOCK_TEST => TestClockHtml,
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

            string local = _angelHubService.IsLocalBoardAvailable ? _angelHubService.LocalBoardFEN : _angelHubService.RemoteBoardFEN;
            string remote = _angelHubService.IsRemoteBoardAvailable ? _angelHubService.RemoteBoardFEN : _angelHubService.LocalBoardFEN;

            byte[] bmpOut = board.ToLowerInvariant() switch
            {
                "local" => _angelHubService.IsLocalBoardAvailable ? await _boardRenderer.GetPngImageDiffFromFenAsync(local, remote, BOARD_IMAGE_SIZE, _angelHubService.IsWhiteOnBottom)
                                                                  : await _boardRenderer.GetPngImageDiffFromFenAsync(BOARD_EMPTY_FEN, BOARD_EMPTY_FEN, BOARD_IMAGE_SIZE, _angelHubService.IsWhiteOnBottom),
                "remote" => _angelHubService.IsRemoteBoardAvailable ? await _boardRenderer.GetPngImageDiffFromFenAsync(remote, local, BOARD_IMAGE_SIZE, _angelHubService.IsWhiteOnBottom)
                                                                    : await _boardRenderer.GetPngImageDiffFromFenAsync(BOARD_EMPTY_FEN, BOARD_EMPTY_FEN, BOARD_IMAGE_SIZE, _angelHubService.IsWhiteOnBottom),
                _ => await _boardRenderer.GetPngImageDiffFromFenAsync(BOARD_EMPTY_FEN, BOARD_EMPTY_FEN, BOARD_IMAGE_SIZE, false)
            };

            return File(bmpOut, MIME_PNG);
        }

        [HttpGet]
        [Route("{action}/{fileName}")]
        public ActionResult Images(string fileName)
        {
            _logger?.LogTrace($"Image requested {fileName}");

            try
            {
                return File(fileName switch
                {
                    SVG_LOGO => SvgLogo,
                    FAV_ICON => FavIcon,
                    _ => throw new FileNotFoundException()
                }, fileName.EndsWith(".svg") ? MIME_SVG : MIME_PNG);
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

            _angelHubService.OnBoardMissmatch += async () =>
            {
                await SendEventResponse(Response, ConstructMessageOnly("OnBoardMissmatch"));
            };

            _angelHubService.OnBoardMatch += async () =>
            {
                await SendEventResponse(Response, ConstructMessageOnly("OnBoardMatch"));
            };

            //Send on connect
            if (_angelHubService.LocalBoardFEN != "")
            {
                await SendEventResponse(Response, JsonSerializer.Serialize(new
                {
                    MessageType = "OnLocalFenChange",
                    BoardFen = _angelHubService.LocalBoardFEN,
                    _angelHubService.IsWhiteOnBottom,
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                }));
            }

            //Send on connect
            if (_angelHubService.RemoteBoardFEN != "")
            {
                await SendEventResponse(Response, JsonSerializer.Serialize(new
                {
                    MessageType = "OnRemoteFenChange",
                    BoardFen = _angelHubService.RemoteBoardFEN,
                    _angelHubService.IsWhiteOnBottom,
                    _angelHubService.LastMove,
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                }));
            }

            _angelHubService.OnOrientationFlipped += async () =>
            {
                await SendEventResponse(Response, JsonSerializer.Serialize(new
                {
                    MessageType = "OnOrientationFlipped",
                    LocalBoardFen = _angelHubService.LocalBoardFEN,
                    RemoteBoardFen = _angelHubService.RemoteBoardFEN,
                    _angelHubService.IsWhiteOnBottom,
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                }));
            };

            _angelHubService.OnClockChange += async () =>
            {
                await SendEventResponse(Response, JsonSerializer.Serialize(new
                {
                    MessageType = "OnClockChange",
                    _angelHubService.WhiteClockMsRemaining,
                    _angelHubService.BlackClockMsRemaining,
                    IsGameActive = _angelHubService.RunWhoString != "3",
                    IsWhiteToPlay = _angelHubService.RunWhoString == "1",
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                }));
            };

            _angelHubService.OnLocalFenChange += async () =>
            {
                await SendEventResponse(Response, JsonSerializer.Serialize(new
                {
                    MessageType = "OnLocalFenChange",
                    BoardFen = _angelHubService.LocalBoardFEN,
                    _angelHubService.IsWhiteOnBottom,
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                }));
            };

            _angelHubService.OnRemoteFenChange += async () =>
            {
                await SendEventResponse(Response, JsonSerializer.Serialize(new
                {
                    MessageType = "OnRemoteFenChange",
                    BoardFen = _angelHubService.RemoteBoardFEN,
                    _angelHubService.IsWhiteOnBottom,
                    _angelHubService.LastMove,
                    IsGameActive = _angelHubService.RunWhoString != "3",
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                }));
            };

            _angelHubService.OnRemoteDisconnect += async () =>
            {
                await SendEventResponse(Response, JsonSerializer.Serialize(new
                {
                    MessageType = "OnRemoteStopWatch",
                    BoardFen = "",
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                }));
            };

            //Keep Alive
            while (true)
            {
                await SendEventResponse(Response, ConstructMessageOnly("Keep-Alive"));
                await Task.Delay(TimeSpan.FromSeconds(KEEP_ALIVE_EVERY_SECONDS));
            }
        }

        //*********************************************//
        #region Event Response Helpers
        private static string ConstructMessageOnly(in string messageType)
        {
            return JsonSerializer.Serialize(new
            {
                MessageType = messageType,
                ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
            });
        }

        private static async Task SendEventResponse(HttpResponse responseObject, string jsonMessage)
        {
            await responseObject.Body.WriteAsync(ASCIIEncoding.ASCII.GetBytes($"data: {jsonMessage}{Environment.NewLine}{Environment.NewLine}"));
            await responseObject.Body.FlushAsync();
        }
        #endregion
        //*********************************************//
    }
}
