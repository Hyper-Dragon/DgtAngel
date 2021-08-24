using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Threading.Tasks;

namespace DynamicBoard
{
    public abstract class BoardRendererBase : IBoardRenderer
    {
        private readonly ILogger _logger;

        public BoardRendererBase(ILogger logger)
        {
            _logger = logger;
            _logger?.LogTrace("Board Render Base Called");
        }

        public abstract Task<Bitmap> GetImageFromFenAsync(string fenString, int imageSize, bool isFromWhitesPerspective);
        public abstract Task<Bitmap> GetImageDiffFromFenAsync(string fenString, string compFenString, int imageSize, bool isFromWhitesPerspective = true);
        public Task<Bitmap> GetImageFromFenSmallAsync(string fenString, bool isFromWhitesPerspective) { return GetImageFromFenAsync(fenString, IBoardRenderer.DEFAULT_BOARD_SIZE_SMALL, isFromWhitesPerspective); }
        public Task<Bitmap> GetImageFromFenMediumAsync(string fenString, bool isFromWhitesPerspective = false) { return GetImageFromFenAsync(fenString, IBoardRenderer.DEFAULT_BOARD_SIZE_MID, isFromWhitesPerspective); }
        public Task<Bitmap> GetImageFromFenLargeAsync(string fenString, bool isFromWhitesPerspective = false) { return GetImageFromFenAsync(fenString, IBoardRenderer.DEFAULT_BOARD_SIZE_LARGE, isFromWhitesPerspective); }
        public Task<Bitmap> GetImageFromFenXLargeAsync(string fenString, bool isFromWhitesPerspective = false) { return GetImageFromFenAsync(fenString, IBoardRenderer.DEFAULT_BOARD_SIZE_X_LARGE, isFromWhitesPerspective); }

    }
}
