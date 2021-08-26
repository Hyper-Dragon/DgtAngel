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

        public Task<Bitmap> GetImageFromFenSmallAsync(in string fenString, in bool isFromWhitesPerspective = true) { return GetImageFromFenAsync(fenString, IBoardRenderer.DEFAULT_BOARD_SIZE_SMALL, isFromWhitesPerspective); }
        public Task<Bitmap> GetImageFromFenMediumAsync(in string fenString, in bool isFromWhitesPerspective = true) { return GetImageFromFenAsync(fenString, IBoardRenderer.DEFAULT_BOARD_SIZE_SMALL, isFromWhitesPerspective); }
        public Task<Bitmap> GetImageFromFenLargeAsync(in string fenString, in bool isFromWhitesPerspective = true) { return GetImageFromFenAsync(fenString, IBoardRenderer.DEFAULT_BOARD_SIZE_SMALL, isFromWhitesPerspective); }
        public Task<Bitmap> GetImageFromFenXLargeAsync(in string fenString, in bool isFromWhitesPerspective = true) { return GetImageFromFenAsync(fenString, IBoardRenderer.DEFAULT_BOARD_SIZE_SMALL, isFromWhitesPerspective); }
        public abstract Task<Bitmap> GetImageFromFenAsync(in string fenString, in int imageSize, in bool isFromWhitesPerspective = true);
        public abstract Task<Bitmap> GetImageDiffFromFenAsync(in string fenString, in string compFenString, in int imageSize, in bool isFromWhitesPerspective = true);
    }
}
