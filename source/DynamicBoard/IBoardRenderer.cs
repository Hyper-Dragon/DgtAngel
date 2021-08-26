using System.Drawing;
using System.Threading.Tasks;

namespace DynamicBoard
{
    public interface IBoardRenderer
    {
        const string BOARD_FEN_INITIAL = @"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";
        const string BOARD_FEN_BLANK = @"8/8/8/8/8/8/8/8";
        const int DEFAULT_BOARD_SIZE_SMALL = 160;
        const int DEFAULT_BOARD_SIZE_MID = 240;
        const int DEFAULT_BOARD_SIZE_LARGE = 480;
        const int DEFAULT_BOARD_SIZE_X_LARGE = 720;

        Task<Bitmap> GetImageFromFenSmallAsync(in string fenString, in bool isFromWhitesPerspective);
        Task<Bitmap> GetImageFromFenMediumAsync(in string fenString, in bool isFromWhitesPerspective);
        Task<Bitmap> GetImageFromFenLargeAsync(in string fenString, in bool isFromWhitesPerspective);
        Task<Bitmap> GetImageFromFenXLargeAsync(in string fenString, in bool isFromWhitesPerspective);
        Task<Bitmap> GetImageFromFenAsync(in string fenString, in int imageSize, in bool isFromWhitesPerspective);
        Task<Bitmap> GetImageDiffFromFenAsync(in string fenString, in string compFenString, in int imageSize, in bool isFromWhitesPerspective);
    }
}
