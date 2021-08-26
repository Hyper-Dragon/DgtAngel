using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using static DynamicBoard.Helpers.FenConversion;

namespace DynamicBoard
{
    public sealed class ShadowBoardRenderer : BoardRendererBase, IBoardRenderer
    {
        private static readonly Dictionary<char, Bitmap> Pieces = new()
        {
            { 'r', ShadowBoardResource.BlackRook },
            { 'b', ShadowBoardResource.BlackBishop },
            { 'n', ShadowBoardResource.BlackKnight },
            { 'k', ShadowBoardResource.BlackKing },
            { 'q', ShadowBoardResource.BlackQueen },
            { 'p', ShadowBoardResource.BlackPawn },
            { 'R', ShadowBoardResource.WhiteRook },
            { 'B', ShadowBoardResource.WhiteBishop },
            { 'N', ShadowBoardResource.WhiteKnight },
            { 'K', ShadowBoardResource.WhiteKing },
            { 'Q', ShadowBoardResource.WhiteQueen },
            { 'P', ShadowBoardResource.WhitePawn },
        };

        private static readonly Dictionary<char, Bitmap> Shadows = new()
        {
            { 'r', ShadowBoardResource.RookShadow },
            { 'b', ShadowBoardResource.BishopShadow },
            { 'n', ShadowBoardResource.KnightShadow },
            { 'k', ShadowBoardResource.KingShadow },
            { 'q', ShadowBoardResource.QueenShadow },
            { 'p', ShadowBoardResource.PawnShadow },
            { 'R', ShadowBoardResource.RookShadow },
            { 'B', ShadowBoardResource.BishopShadow },
            { 'N', ShadowBoardResource.KnightShadow },
            { 'K', ShadowBoardResource.KingShadow },
            { 'Q', ShadowBoardResource.QueenShadow },
            { 'P', ShadowBoardResource.PawnShadow },
        };

        private readonly ILogger<ChessDotComBoardRenderer> _logger;
        private static readonly object gdiLock = new();

        public ShadowBoardRenderer(ILogger<ChessDotComBoardRenderer> logger) : base(logger)
        {
            _logger = logger;
        }

        public override Task<Bitmap> GetImageFromFenAsync(in string fenString, in int imageSize, in bool isFromWhitesPerspective = true)
        {
            return GetImageDiffFromFenAsync(fenString, fenString, imageSize, isFromWhitesPerspective);
        }

        public override Task<Bitmap> GetImageDiffFromFenAsync(in string fenString = "", in string compFenString = "", in int imageSize = 1024, in bool isFromWhitesPerspective = true)
        {
            _logger?.LogDebug($"Constructing board for fen [{fenString}]");

            Bitmap errorBmpOut = null;
            Bitmap resizedBmpOut = null;

            try
            {
                Monitor.Enter(gdiLock);

                // TODO: add switch colour profile
                // Dont pass in the blank board unless customised...it is slower!
                resizedBmpOut = RenderBoard(//blankBoard: blankBoard,
                                            //whiteSquareColour: Color.FromArgb(255, 238, 238, 210),
                                            //blackSquareColour: Color.FromArgb(255, 118, 150, 86),
                                            //errorSquareColour: Color.FromArgb(150, Color.Red),
                                            whiteSquareColour: Color.PaleTurquoise,
                                            blackSquareColour: Color.DarkCyan,
                                            errorSquareColour: Color.FromArgb(150, Color.Yellow),
                                            fenString: fenString,
                                            fenCompareString: compFenString,
                                            requestedSizeOut: imageSize,
                                            internalRenderSize: 3,
                                            requestHighQualityComposite: true,
                                            shadowOffsetX: 5,
                                            shadowOffsetY: 5,
                                            isFlipRequired: !isFromWhitesPerspective
                                            );

            }
            catch (Exception ex)
            {
                _logger?.LogDebug($"Rendering board error [{fenString}] [{ex.Message}]");

                Bitmap bmp = new(imageSize, imageSize);
                using Graphics g = Graphics.FromImage(bmp);
                g.DrawString($"ERROR RENDERING IMAGE{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}", new Font("Segoe UI", 12), Brushes.Black, new PointF(5, 20));
                g.Flush();
                errorBmpOut = bmp;
                resizedBmpOut = null;
            }
            finally
            {
                Monitor.Exit(gdiLock);
            }

            return Task.FromResult(resizedBmpOut ?? errorBmpOut);
        }

        private static Bitmap RenderBoard(in Bitmap blankBoard = null, // Slower than passing colours
                                          in Color? whiteSquareColour = null,
                                          in Color? blackSquareColour = null,
                                          in Color? errorSquareColour = null,
                                          in string fenString = "8/8/8/8/8/8/8/8",
                                          in string fenCompareString = "",
                                          in int requestedSizeOut = 0,
                                          in int internalRenderSize = 1, // 1 is best
                                          in bool requestHighQualityComposite = true,
                                          in int shadowOffsetX = 25,
                                          in int shadowOffsetY = 15,
                                          in bool isFlipRequired = false)
        {
            // Make the max return size the natural draw size
            int squareSize = 320 / Math.Min(10, internalRenderSize); // 320 matches the piece graphic size
            int boardSize = squareSize * 8;
            int sizeOut = requestedSizeOut == 0 ? boardSize : Math.Min(requestedSizeOut, boardSize);

            char[] fenOut = FenToCharArray(fenString, isFlipRequired);
            char[] fenOutComp = string.IsNullOrWhiteSpace(fenCompareString) ? fenOut : FenToCharArray(fenCompareString, isFlipRequired);

            Brush whiteSqBrush = new SolidBrush(whiteSquareColour ?? Color.White);
            Brush errorBrush = new SolidBrush(errorSquareColour ?? Color.Red);

            using Bitmap boardBmp = blankBoard is null ? new(boardSize, boardSize) : new(blankBoard);
            using Graphics graphics = Graphics.FromImage(boardBmp);

            graphics.SmoothingMode = requestHighQualityComposite ? SmoothingMode.HighQuality : SmoothingMode.HighSpeed;
            graphics.CompositingQuality = requestHighQualityComposite ? CompositingQuality.HighQuality : CompositingQuality.HighSpeed;

            // Render the squares (if a pre-rendered board was not passed in)...
            if (blankBoard is null)
            {
                graphics.Clear(blackSquareColour ?? Color.Black);

                for (int row = 0; row < 8; row++)
                {
                    for (int col = 0; col < 8; col++)
                    {
                        if ((row % 2 == 0 && col % 2 == 0) || (row % 2 != 0 && col % 2 != 0))
                        {
                            graphics.FillRectangle(whiteSqBrush, col * squareSize, row * squareSize, squareSize, squareSize);
                        }
                    }
                }
            }

            // ...and add the pieces...
            for (int row = 0, posCount = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++, posCount++)
                {
                    //...highlighting differences...
                    if (errorSquareColour.HasValue && fenOut[posCount] != fenOutComp[posCount])
                    {
                        graphics.FillRectangle(errorBrush,
                                               col * squareSize,
                                               row * squareSize,
                                               squareSize,
                                               squareSize);
                    }

                    //...and finallay adding the pieces
                    if (Pieces.ContainsKey(fenOut[posCount]))
                    {
                        if (shadowOffsetX > 0 || shadowOffsetY > 0)
                        {
                            graphics.DrawImage(Shadows[fenOut[posCount]], (col * squareSize) + shadowOffsetX, (row * squareSize) + shadowOffsetY, squareSize, squareSize);
                        }

                        graphics.DrawImage(Pieces[fenOut[posCount]], col * squareSize, row * squareSize, squareSize, squareSize);
                    }
                }
            }

            //Keeps the memory in check
            GC.Collect();

            return new(boardBmp, new Size(sizeOut, sizeOut));
        }

    }
}
