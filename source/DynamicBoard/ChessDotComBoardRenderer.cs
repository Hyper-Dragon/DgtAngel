using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicBoard
{
    public sealed class ChessDotComBoardRenderer : BoardRendererBase, IBoardRenderer
    {
        private readonly ILogger<ChessDotComBoardRenderer> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private const string BOARD_DOWNLOAD_SIZE = "2";
        private const string BOARD_URL_START = @"https://www.chess.com/dynboard?fen=";
        private const string BOARD_URL_OPT = @"&board=green&piece=space&size=" + BOARD_DOWNLOAD_SIZE;
        private const int DL_TIMEOUT = 3000;

        public ChessDotComBoardRenderer(ILogger<ChessDotComBoardRenderer> logger,
                                        IHttpClientFactory httpClientFactory) : base(logger)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public override Task<Bitmap> GetImageFromFenAsync(in string fenString, in int imageSize, in bool isFromWhitesPerspective = true)
        {
            //No support for this so just return the regular fen
            return GetImageFromFenAsync(fenString, imageSize, isFromWhitesPerspective);
        }

        public override Task<Bitmap> GetImageDiffFromFenAsync(in string fenString = "", in string compFenString = "", in int imageSize = 1024, in bool isFromWhitesPerspective = true)
        {
            _logger?.LogDebug($"Constructing board for fen [{fenString}]");

            HttpClient httpClient = _httpClientFactory.CreateClient();
            Bitmap resizedBmpOut;

            try
            {
                CancellationToken canxToken = new();
                // TODO: Verify this
                // DONT....{HttpUtility.UrlEncode(fenString)} - The endpoint doesn't decode the url properly!
                //string boardUrl = $"{BOARD_URL_START}{fenString}{BOARD_URL_OPT}{(isFromWhitesPerspective ? "" : "&flip=true")}";
                string boardUrl = $"{BOARD_URL_START}{fenString}{BOARD_URL_OPT}{(isFromWhitesPerspective ? "" : "&flip=true")}";

                _logger?.LogDebug($"Downloading board image from [{boardUrl}]");
                Task<System.IO.Stream> responseTask = httpClient.GetStreamAsync(new Uri(boardUrl), canxToken);
                responseTask.Wait(DL_TIMEOUT);

                using Image bmp = Bitmap.FromStream(responseTask.Result);
                resizedBmpOut = new Bitmap(bmp, new Size(imageSize, imageSize));
            }
            catch (OperationCanceledException)
            {
                _logger?.LogDebug($"Download fom Chess.com timedout [{fenString}]");

                Bitmap bmp = new(imageSize, imageSize);
                using Graphics g = Graphics.FromImage(bmp);
                g.DrawString($"Timeout From Chess.com", new Font("Segoe UI", 8), Brushes.Black, new PointF(5, 20));
                g.Flush();
                resizedBmpOut = bmp;
            }
            catch (Exception ex)
            {
                _logger?.LogDebug($"Download from Chess.com error [{fenString}] [{ex.Message}]");

                Bitmap bmp = new(imageSize, imageSize);
                using Graphics g = Graphics.FromImage(bmp);
                g.DrawString($"ERROR GETTING IMAGE{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}", new Font("Segoe UI", 8), Brushes.Black, new PointF(5, 20));
                g.Flush();
                resizedBmpOut = bmp;
            }

            return Task.FromResult(resizedBmpOut);
        }
    }
}
