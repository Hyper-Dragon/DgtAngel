using Microsoft.Extensions.Logging;

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
        public abstract Task<byte[]> GetPngImageFromFenAsync(in string fenString, in int imageSize, in bool isFromWhitesPerspective = true);
        public abstract Task<byte[]> GetPngImageDiffFromFenAsync(in string fenString, in string compFenString, in int imageSize, in bool isFromWhitesPerspective = true);
    }
}
