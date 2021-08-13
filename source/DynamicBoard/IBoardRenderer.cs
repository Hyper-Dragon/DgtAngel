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

        Task<Bitmap> GetImageFromFenSmallAsync(string fenString, bool isFromWhitesPerspective = true);
        Task<Bitmap> GetImageFromFenMediumAsync(string fenString, bool isFromWhitesPerspective = true);
        Task<Bitmap> GetImageFromFenLargeAsync(string fenString, bool isFromWhitesPerspective = true);
        Task<Bitmap> GetImageFromFenXLargeAsync(string fenString, bool isFromWhitesPerspective = true);
        Task<Bitmap> GetImageFromFenAsync(string fenString, int imageSize, bool isFromWhitesPerspective = true);
    }
}
