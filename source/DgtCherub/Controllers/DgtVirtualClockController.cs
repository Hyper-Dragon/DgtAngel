using DgtCherub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
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
    public class DgtVirtualClockController : ControllerBase
    {
        private const string RESOURCE_CLOCK_ROOT = "DgtCherub.Assets.Clocks";
        private const string RESOURCE_CLOCK_NAME = "SlideClock";
        private const string RESOURCE_CLOCK_HTML = "Clock.html";
        private const string RESOURCE_CLOCK_FAV = "favicon.png";
        private const string RESOURCE_CLOCK_SVG = "DgtAngelLogo.svg";

        private readonly ILogger _logger;
        private readonly IAppDataService _appDataService;

        private readonly string IndexPageHtml;
        private readonly byte[] FavIcon;
        private readonly byte[] SvgLogo;

        public DgtVirtualClockController(ILogger<Form1> logger, IAppDataService appData)
        {
            _logger = logger;
            _appDataService = appData;

            using System.IO.Stream indexHtmlStream = Assembly.GetExecutingAssembly()
                                                             .GetManifestResourceStream($"{RESOURCE_CLOCK_ROOT}.{RESOURCE_CLOCK_NAME}.{RESOURCE_CLOCK_HTML}");

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

#pragma warning disable CA1822 // DO NOT Mark members as static - it's part of the API!
        [HttpGet]
        [Route("{action}/{string1}/{string2}/{int1:int}")]
        public object TestResponse(string string1 = "none", string string2 = "none", int int1 = -1)

        {
            //  curl http://localhost:37964/DgtVirtualClock/TestResponse/st1a/st2a/3 ; echo
            return new { TestString1 = string1, TestString2 = string2, TestInt1 = int1, CalledAt = System.DateTime.Now };
        }
#pragma warning restore CA1822



        [HttpGet]
        [Route("{action}")]
        public ContentResult GetClock()
        {
            // http://localhost:37964/DgtVirtualClock/GetClock

            //TODO: Replace with embeded resource
            //string htmlOut = System.IO.File.ReadAllText(@"C:/TESTHTML2/tryagain.html");

            string htmlOut = IndexPageHtml;

            return new ContentResult
            {
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
                Content = htmlOut,
            };
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
            // http://localhost:37964/DgtVirtualClock/GetStuff

            int clientServerTimeDiff = (int)(double.Parse(clientUtcMs) - DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds);

            //TODO: Need to get the time the clocks were taken
            Response.Headers.Add("Content-Type", "text/event-stream");

            _appDataService.OnClockChange += () =>
            {
                string[] wcs = _appDataService.WhiteClock.Split(':');
                string[] bcs = _appDataService.BlackClock.Split(':');

                //TODO: Knockd off a couple of seconds for now....fix properly
                int wctime = ((((int.Parse(wcs[0]) * 60) * 60) + (int.Parse(wcs[1]) * 60) + int.Parse(wcs[2])) * 1000) - 2000;
                int bctime = ((((int.Parse(bcs[0]) * 60) * 60) + (int.Parse(bcs[1]) * 60) + int.Parse(bcs[2])) * 1000) - 2000;

                _appDataService.OnClockChange += async () =>
                {
                    string jsonString = JsonSerializer.Serialize(new
                    {
                        MessageType = "OnClockChange",
                        WhiteClockMsRemaining = wctime,
                        BlackClockMsRemaining = bctime,
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
            };

            _appDataService.OnLocalFenChange += async () =>
            {
                string jsonString = JsonSerializer.Serialize(new
                {
                    MessageType = "OnLocalFenChange",
                    BoardFen = _appDataService.LocalBoardFEN,
                    ResponseAtData = $"{System.DateTime.Now.ToShortDateString()}",
                    ResponseAtTime = $"{System.DateTime.Now.ToLongTimeString()}",
                });

                string dataItem = $"data: {jsonString}{Environment.NewLine}{Environment.NewLine}";
                byte[] dataItemBytes = ASCIIEncoding.ASCII.GetBytes(dataItem);
                await Response.Body.WriteAsync(dataItemBytes);
                await Response.Body.FlushAsync();
            };

            _appDataService.OnChessDotComFenChange += async () =>
            {
                string jsonString = JsonSerializer.Serialize(new
                {
                    MessageType = "OnRemoteFenChange",
                    BoardFen = _appDataService.ChessDotComBoardFEN,
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
