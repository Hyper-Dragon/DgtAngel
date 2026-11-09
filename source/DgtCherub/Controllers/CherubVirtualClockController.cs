using DgtCherub.Helpers;
using DgtCherub.Services;
using DynamicBoard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;
using UciComms;
using static DgtCherub.Helpers.ResourceLoader;

namespace DgtCherub.Controllers
{
#pragma warning disable AsyncFixer03 // Fire-and-forget async-void methods or delegates
    [Route("[controller]")]
    [Controller]
    public sealed class CherubVirtualClockController : ControllerBase
    {
        private const string RESOURCE_CLOCK_ROOT = "DgtCherub.Assets.Clocks";
        private const string RESOURCE_CLOCK_HTML = "Clock.html";
        private const string FAV_ICON = "favicon.png";
        private const string SVG_LOGO = "DgtAngelLogo.svg";

        private const string CLOCK_SLIDE = "GreySlide";
        private const string CLOCK_FIXED = "GreyFixed";
        private const string CLOCK_LOCAL = "LocalBroadcast";
        private const string CLOCK_REMOTE = "RemoteBroadcast";
        private const string CLOCK_WINGED_HORSE = "WingedHorse";

        private const string RESOURCE_CLOCK_SLIDE = $"{RESOURCE_CLOCK_ROOT}.{CLOCK_SLIDE}.{RESOURCE_CLOCK_HTML}";
        private const string RESOURCE_CLOCK_FIXED = $"{RESOURCE_CLOCK_ROOT}.{CLOCK_FIXED}.{RESOURCE_CLOCK_HTML}";
        private const string RESOURCE_CLOCK_LOCAL = $"{RESOURCE_CLOCK_ROOT}.{CLOCK_LOCAL}.{RESOURCE_CLOCK_HTML}";
        private const string RESOURCE_CLOCK_REMOTE = $"{RESOURCE_CLOCK_ROOT}.{CLOCK_REMOTE}.{RESOURCE_CLOCK_HTML}";
        private const string RESOURCE_CLOCK_WINGED_HORSE = $"{RESOURCE_CLOCK_ROOT}.{CLOCK_WINGED_HORSE}.{RESOURCE_CLOCK_HTML}";

        private const string RESOURCE_CLOCK_INDEX = $"{RESOURCE_CLOCK_ROOT}.index.html";
        private const string RESOURCE_CLOCK_FAV = $"{RESOURCE_CLOCK_ROOT}.{FAV_ICON}";
        private const string RESOURCE_CLOCK_SVG = $"{RESOURCE_CLOCK_ROOT}.{SVG_LOGO}";

        private const string MIME_HTM = "text/html";
        private const string MIME_PNG = "image/png";
        private const string MIME_SVG = "image/svg+xml";
        private const string MIME_CSS = "text/css";
        private const string MIME_JS = "text/javascript";
        private const string MIME_UNITY = "application/wasm";
        private const string MIME_EVENT = "text/event-stream";

        private const string BOARD_EMPTY_FEN = "8/8/8/8/8/8/8/8";
        private const int BOARD_IMAGE_SIZE = 1024;

        private const double KEEP_ALIVE_EVERY_SECONDS = 60;

        private readonly DateTime unixDateTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly ILogger _logger;
        private readonly IAngelHubService _angelHubService;
        private readonly IBoardRenderer _boardRenderer;
        private readonly IUciEngineManager _uciEngineManager;

        private readonly string IndexPageHtml;
        private readonly string SlideClockHtml;
        private readonly string FixedClockHtml;
        private readonly string LocalClockHtml;
        private readonly string RemoteClockHtml;
        private readonly string WigedHorseHtml;
        private readonly byte[] FavIcon;
        private readonly byte[] SvgLogo;

        private static long LastSeenEventTimeTicks = long.MinValue;

        public CherubVirtualClockController(ILogger<CherubVirtualClockController> logger, IAngelHubService appData, IBoardRenderer boardRenderer, IUciEngineManager uciEngineManager)
        {
            _logger = logger;
            _angelHubService = appData;
            _boardRenderer = boardRenderer;
            _uciEngineManager = uciEngineManager;

            IndexPageHtml = LoadResourceString(RESOURCE_CLOCK_INDEX);
            SlideClockHtml = LoadResourceString(RESOURCE_CLOCK_SLIDE);
            FixedClockHtml = LoadResourceString(RESOURCE_CLOCK_FIXED);
            LocalClockHtml = LoadResourceString(RESOURCE_CLOCK_LOCAL);
            RemoteClockHtml = LoadResourceString(RESOURCE_CLOCK_REMOTE);
            WigedHorseHtml = LoadResourceString(RESOURCE_CLOCK_WINGED_HORSE);

            FavIcon = LoadResource(RESOURCE_CLOCK_FAV);
            SvgLogo = LoadResource(RESOURCE_CLOCK_SVG);
        }

        [HttpGet]
        [Route("/")]
        public ContentResult GetIndex()
        {
            // http://localhost:37964/
            _logger?.LogTrace("Clock index requested");

            NoPageCache();

            return new ContentResult
            {
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
                Content = IndexPageHtml,
            };
        }

        /// <summary>
        /// Tells the browser not to keep a copy of a clock page.
        /// </summary>
        /// <remarks>
        /// The pages are embedded in the assembly, so a rebuild changes what they contain without
        /// changing their address. A cached page then runs yesterday code against today messages:
        /// it silently ignores anything it does not recognise, and the only symptom is a screen
        /// that will not do what the code plainly does.
        /// </remarks>
        private void NoPageCache()
        {
            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";
        }


        [HttpGet]
        [Route("{action}/{clock}")]
        public ContentResult GetClock(string clock)
        {
            ArgumentNullException.ThrowIfNull(clock);

            // http://localhost:37964/CherubVirtualClock/GetClock
            _logger?.LogTrace($"Clock requested [{clock.SanitiseText()}]");

            NoPageCache();

            try
            {
                return new ContentResult
                {
                    ContentType = MIME_HTM,
                    StatusCode = (int)HttpStatusCode.OK,
                    Content = clock switch
                    {
                        CLOCK_SLIDE => SlideClockHtml,
                        CLOCK_FIXED => FixedClockHtml,
                        CLOCK_LOCAL => LocalClockHtml,
                        CLOCK_REMOTE => RemoteClockHtml,
                        CLOCK_WINGED_HORSE => WigedHorseHtml,
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
        [Route("/CherubVirtualClock/{action}/{clock}/{directory}/{filename}")]
        public ActionResult GetFile(string clock, string directory, string filename)
        {
            ArgumentNullException.ThrowIfNull(filename);

            try
            {
                string mimeType = filename.Split(".").Last() switch
                {
                    "html" => MIME_HTM,
                    "png" => MIME_PNG,
                    "svg" => MIME_SVG,
                    "css" => MIME_CSS,
                    "js" => MIME_JS,
                    "unityweb" => MIME_UNITY,
                    _ => throw new FileNotFoundException()
                };

                //http://127.0.0.1:37964/CherubVirtualClock/GetClock/TemplateData/style.css

                // A clock or asset the build does not carry is a bad URL, not a server fault.
                if (!TryLoadResource($"{RESOURCE_CLOCK_ROOT}.{clock}.{directory}.{filename}", out byte[] content))
                {
                    throw new FileNotFoundException();
                }

                return File(content, mimeType);
            }
            catch (FileNotFoundException)
            {
                return StatusCode(404);
            }
        }

        [HttpGet]
        [Route("{action}/{board}/{localBoard}/{remoteBoard}")]
        public async Task<FileContentResult> BoardImageCompareByFen(string board, string localBoard, string remoteBoard)
        {
            ArgumentNullException.ThrowIfNull(board);

            _logger?.LogTrace($"Board image requested [{localBoard} :: {remoteBoard}]".SanitiseText());

            string local = string.IsNullOrEmpty(localBoard) ? HttpUtility.UrlDecode(remoteBoard) : HttpUtility.UrlDecode(localBoard);
            string remote = string.IsNullOrEmpty(remoteBoard) ? HttpUtility.UrlDecode(localBoard) : HttpUtility.UrlDecode(remoteBoard);

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
        [Route("{action}/{board}")]
        public async Task<FileContentResult> BoardImage(string board)
        {
            ArgumentNullException.ThrowIfNull(board);

            _logger?.LogTrace($"Board image requested [{board.SanitiseText()}]");

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
        [Route("{action}/{board}")]
        public async Task<FileContentResult> BoardImageNoCompare(string board)
        {
            ArgumentNullException.ThrowIfNull(board);

            _logger?.LogTrace($"Board image without compare requested [{board.SanitiseText()}]");;

            string local = _angelHubService.IsLocalBoardAvailable ? _angelHubService.LocalBoardFEN : _angelHubService.RemoteBoardFEN;
            string remote = _angelHubService.IsRemoteBoardAvailable ? _angelHubService.RemoteBoardFEN : _angelHubService.LocalBoardFEN;

            byte[] bmpOut = board.ToLowerInvariant() switch
            {
                "local" => _angelHubService.IsLocalBoardAvailable ? await _boardRenderer.GetPngImageDiffFromFenAsync(local, local, BOARD_IMAGE_SIZE, _angelHubService.IsWhiteOnBottom)
                                                                  : await _boardRenderer.GetPngImageDiffFromFenAsync(BOARD_EMPTY_FEN, BOARD_EMPTY_FEN, BOARD_IMAGE_SIZE, _angelHubService.IsWhiteOnBottom),
                "remote" => _angelHubService.IsRemoteBoardAvailable ? await _boardRenderer.GetPngImageDiffFromFenAsync(remote, remote, BOARD_IMAGE_SIZE, _angelHubService.IsWhiteOnBottom)
                                                                    : await _boardRenderer.GetPngImageDiffFromFenAsync(BOARD_EMPTY_FEN, BOARD_EMPTY_FEN, BOARD_IMAGE_SIZE, _angelHubService.IsWhiteOnBottom),
                _ => await _boardRenderer.GetPngImageDiffFromFenAsync(BOARD_EMPTY_FEN, BOARD_EMPTY_FEN, BOARD_IMAGE_SIZE, false)
            };

            return File(bmpOut, MIME_PNG);
        }

        [HttpGet]
        [Route("{action}/{fileName}")]
        public ActionResult Images(string fileName)
        {
            ArgumentNullException.ThrowIfNull(fileName);

            _logger?.LogTrace($"Image requested [{fileName.SanitiseText()}]");

            try
            {
                return File(fileName switch
                {
                    SVG_LOGO => SvgLogo,
                    FAV_ICON => FavIcon,
                    _ => throw new FileNotFoundException()
                }, fileName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ? MIME_SVG : MIME_PNG);
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
            ArgumentNullException.ThrowIfNull(clientUtcMs);

            // http://localhost:37964/CherubVirtualClock/GetStuff
            _logger?.LogTrace($"Clock client connected running [{clientUtcMs.SanitiseText()}]");

            // The reading is only good for one thing: showing how far the client's clock is
            // from this machine's. It arrives on a URL, so it is parsed defensively - an
            // unhandled FormatException here faulted the clock stream for a bad address,
            // and the stream does not depend on the value at all.
            if (!double.TryParse(clientUtcMs, NumberStyles.Float, CultureInfo.InvariantCulture, out double clientTimeMs))
            {
                _logger?.LogTrace($"Clock client sent a time that is not a number [{clientUtcMs.SanitiseText()}]");
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            int clientServerTimeDiff = (int)(clientTimeMs - DateTime.Now.ToUniversalTime().Subtract(unixDateTime).TotalMilliseconds);
            _logger?.LogTrace($"Clock client skew [{clientServerTimeDiff}] ms against this machine");

            // Set the header rather than adding one: Add throws when the key is already
            // present (ASP0019), and the response is not ours alone to guarantee that.
            Response.ContentType = MIME_EVENT;

            _angelHubService.OnBoardMissmatch += async (sentTimeTicks, _, _, _) =>
            {
                if (sentTimeTicks > LastSeenEventTimeTicks)
                {
                    LastSeenEventTimeTicks = sentTimeTicks;
                    await SendEventResponse(Response, ConstructMessageOnly("OnBoardMissmatch"));
                }
            };

            _angelHubService.OnBoardMatchFromMissmatch += async (sentTimeTicks) =>
            {
                if (sentTimeTicks > LastSeenEventTimeTicks)
                {
                    LastSeenEventTimeTicks = sentTimeTicks;
                    await SendEventResponse(Response, ConstructMessageOnly("OnBoardMatch"));
                }
            };

            _uciEngineManager.OnLoadedEngineBoardEvalChanged += async (uciEngineEval) =>
            {
                await SendEventResponse(Response, JsonSerializer.Serialize(new
                {
                    MessageType = "OnBoardEvalChanged",
                    uciEngineEval.Eval,
                    uciEngineEval.MateIn,
                    uciEngineEval.BestMove,
                    uciEngineEval.Depth,
                    // Ready-to-display values. The clock pages show exactly what the panel in
                    // Cherub shows, and neither has to re-derive a score or a variation.
                    Score = KibitzerDisplay.FormatScore(uciEngineEval),
                    Lines = KibitzerDisplay.GetLines(_angelHubService.CurrentKibitzerPosition, uciEngineEval),
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                }));
            };

            _angelHubService.OnKibitzerPositionChange += async (ChessHelpers.KibitzerPosition position) =>
            {
                await SendEventResponse(Response, JsonSerializer.Serialize(new
                {
                    MessageType = "OnKibitzerPosition",
                    position.IsWhiteToPlay,
                    position.CastleField,
                    position.EnPassantSquare,
                    position.Fen,
                    Info = KibitzerDisplay.FormatInfo(position, null),
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                }));
            };

            _uciEngineManager.OnUciEngineLoaded += async (UciChessEngine engine) =>
            {
                await SendEventResponse(Response, JsonSerializer.Serialize(new
                {
                    MessageType = "OnUciEngineLoaded",
                    _uciEngineManager.LoadedEngineName,
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                }));
            };


            _uciEngineManager.OnUciEngineReleased += (string engineName) =>
            {
                //await SendEventResponse(Response, JsonSerializer.Serialize(new
                //{
                //    MessageType = "OnUciEngineReleased",
                //    Name = engineName,
                //    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                //    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                //}));
            };

            _uciEngineManager.OnUciEngineStartError += async (string errorMsg) =>
            {
                await SendEventResponse(Response, JsonSerializer.Serialize(new
                {
                    MessageType = "OnUciEngineStartError",
                    Name = errorMsg,
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                }));
            };

            _uciEngineManager.OnKibitzerFenChange += async (string fen) =>
            {
                await SendEventResponse(Response, JsonSerializer.Serialize(new
                {
                    MessageType = "OnKibitzerFenChange",
                    Fen = fen,
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                }));
            };

            // The page reads the kibitzer state when it connects, which is no use to a screen that
            // was already open when the kibitzer was switched on: it went on saying "kibitzer off"
            // while scores arrived underneath it. The switch only ever goes on, so activation is
            // the only change there is to report.
            _uciEngineManager.OnKibitzerActivated += async () => await SendKibitzerReset();


            //Send on connect. The page keeps whatever the last session left on it, so it is told
            //to clear itself and what the current state is before anything live arrives.
            await SendKibitzerReset();

            //Send on connect. Without this a clock opened after the engine was loaded never
            //learns which engine is running, because the load happened before it was listening.
            if (_uciEngineManager.LoadedEngineName != "NO ENGINE")
            {
                await SendEventResponse(Response, JsonSerializer.Serialize(new
                {
                    MessageType = "OnUciEngineLoaded",
                    _uciEngineManager.LoadedEngineName,
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                }));
            }

            //Send on connect
            if (_uciEngineManager.IsKibitzerRunning && _angelHubService.CurrentKibitzerPosition != null)
            {                await SendEventResponse(Response, JsonSerializer.Serialize(new
                {
                    MessageType = "OnKibitzerPosition",
                    _angelHubService.CurrentKibitzerPosition.IsWhiteToPlay,
                    _angelHubService.CurrentKibitzerPosition.CastleField,
                    _angelHubService.CurrentKibitzerPosition.EnPassantSquare,
                    _angelHubService.CurrentKibitzerPosition.Fen,
                    Info = KibitzerDisplay.FormatInfo(_angelHubService.CurrentKibitzerPosition, null),
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                }));
            }

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

            _angelHubService.OnLocalFenChange += async (string localFen) =>
            {
                await SendEventResponse(Response, JsonSerializer.Serialize(new
                {
                    MessageType = "OnLocalFenChange",
                    BoardFen = localFen,
                    _angelHubService.IsWhiteOnBottom,
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                }));
            };

            _angelHubService.OnRemoteFenChange += async (string fromRemoteFen, string toRemoteFen, string lastMove, string clockFen, string boardFen, string _, bool _) =>
            {
                await SendEventResponse(Response, JsonSerializer.Serialize(new
                {
                    MessageType = "OnRemoteFenChange",
                    BoardFen = toRemoteFen,
                    _angelHubService.IsWhiteOnBottom,
                    lastMove,
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

            //Keep Alive, and stop when the client goes away. Without the cancellation this
            //loop outlives its request: SendEventResponse swallows every failed write, so a
            //client that disconnected would leave a loop writing to a dead response every
            //minute for the life of the process - and every reconnection added another.
            CancellationToken clientGone = HttpContext?.RequestAborted ?? CancellationToken.None;

            while (!clientGone.IsCancellationRequested)
            {
                await SendEventResponse(Response, ConstructMessageOnly("Keep-Alive"));

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(KEEP_ALIVE_EVERY_SECONDS), clientGone);
                }
                catch (OperationCanceledException)
                {
                    //The client has gone; the stream ends here.
                    break;
                }
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

        /// <summary>
        /// Tells the page which engine is loaded and whether the kibitzer is running.
        /// </summary>
        /// <remarks>
        /// The page uses this one message for both jobs: it names the engine in the header and
        /// decides whether to offer the panel's advice or wait for a score. It is sent when a page
        /// connects and again when the kibitzer is switched on, because the switch only goes one
        /// way and that is the only change there is to report.
        /// </remarks>
        private Task SendKibitzerReset()
        {
            return SendEventResponse(Response, JsonSerializer.Serialize(new
            {
                MessageType = "OnKibitzerReset",
                UciEngineName = _uciEngineManager.LoadedEngineName == "NO ENGINE" ? "" : _uciEngineManager.LoadedEngineName,
                _uciEngineManager.IsKibitzerRunning,
                ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
            }));
        }




        private static async Task SendEventResponse(HttpResponse responseObject, string jsonMessage)
        {
            try
            {
                await responseObject.Body.WriteAsync(ASCIIEncoding.ASCII.GetBytes($"data: {jsonMessage}{Environment.NewLine}{Environment.NewLine}"));
                await responseObject.Body.FlushAsync();
            }
            catch (Exception)
            {
                //Do nothing - all these calls are async and the client may have disconnected
            }
        }
        #endregion
        //*********************************************//
    }
}
