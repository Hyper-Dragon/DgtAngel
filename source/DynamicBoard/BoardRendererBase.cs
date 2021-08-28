using Microsoft.Extensions.Logging;
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

        public Task<byte[]> GetImageFromFenSmallAsync(in string fenString, in bool isFromWhitesPerspective = true) { return GetPngImageFromFenAsync(fenString, IBoardRenderer.DEFAULT_BOARD_SIZE_SMALL, isFromWhitesPerspective); }
        public Task<byte[]> GetImageFromFenMediumAsync(in string fenString, in bool isFromWhitesPerspective = true) { return GetPngImageFromFenAsync(fenString, IBoardRenderer.DEFAULT_BOARD_SIZE_SMALL, isFromWhitesPerspective); }
        public Task<byte[]> GetImageFromFenLargeAsync(in string fenString, in bool isFromWhitesPerspective = true) { return GetPngImageFromFenAsync(fenString, IBoardRenderer.DEFAULT_BOARD_SIZE_SMALL, isFromWhitesPerspective); }
        public Task<byte[]> GetImageFromFenXLargeAsync(in string fenString, in bool isFromWhitesPerspective = true) { return GetPngImageFromFenAsync(fenString, IBoardRenderer.DEFAULT_BOARD_SIZE_SMALL, isFromWhitesPerspective); }
        public abstract Task<byte[]> GetPngImageFromFenAsync(in string fenString, in int imageSize, in bool isFromWhitesPerspective = true);
        public abstract Task<byte[]> GetPngImageDiffFromFenAsync(in string fenString, in string compFenString, in int imageSize, in bool isFromWhitesPerspective = true);
    }
}
