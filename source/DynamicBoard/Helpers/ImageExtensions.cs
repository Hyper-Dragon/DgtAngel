using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

namespace DynamicBoard.Helpers
{
    [SupportedOSPlatform("windows")]
    public static class ImageExtensions
    {
        public static byte[] ConvertToPngByteArray(this Bitmap bmp)
        {
            MemoryStream memoryStream = new();

            bmp.Save(memoryStream, ImageFormat.Png);
            return memoryStream.ToArray();
        }

        public static Bitmap ConvertPngByteArrayToBitmap(this byte[] pngArray)
        {
            return (Bitmap)Bitmap.FromStream(new MemoryStream(pngArray));
        }
    }
}
