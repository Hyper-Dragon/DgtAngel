using DynamicBoard;
using Microsoft.Extensions.Logging.Abstractions;

namespace DynamicBoardTest.TestSupport
{
    /// <summary>
    /// Builds the renderer under test.
    /// </summary>
    /// <remarks>
    /// ShadowBoardRenderer draws from embedded piece art and touches no network, so tests
    /// using it are fast and independent of any third party. The Chess.com-backed renderer
    /// that used to live beside it was removed: it was never registered in the application,
    /// and it ignored the comparison board, so it could not show a mismatch at all.
    /// </remarks>
    public static class RendererFactory
    {
        public static IBoardRenderer CreateShadowRenderer()
            => new ShadowBoardRenderer(NullLogger<ShadowBoardRenderer>.Instance);
    }
}
