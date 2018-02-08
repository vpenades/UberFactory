
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.MetaData;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;

namespace Epsylon.ImageSharp.Procedural
{
    public static class CheckersFactory
    {        

        public static IImageProcessingContext<TPixel> FillCheckers<TPixel>(this IImageProcessingContext<TPixel> source, int cellWidth, int cellHeight, TPixel oddColor, TPixel evenColor) where TPixel : struct, IPixel<TPixel>
        {
            return source.Apply(img => img._FillCheckers(cellWidth, cellHeight, oddColor, evenColor));
        }

        static void _FillCheckers<TPixel>(this Image<TPixel> image, int cellWidth, int cellHeight, TPixel oddColor, TPixel evenColor) where TPixel : struct, IPixel<TPixel>
        {
            // related to SixLabors.ImageSharp.Drawing.Brushes.PatternBrush ??

            for (int y=0; y < image.Height; ++y)
            {
                for(int x=0; x < image.Width; ++x)
                {
                    Math.DivRem(x, cellWidth  * 2, out int bx);
                    Math.DivRem(y, cellHeight * 2, out int by);

                    var b = (bx < cellWidth) ^ (by < cellHeight);

                    image[x,y] = b ? oddColor : evenColor;
                }
            }
        }
        
    }
}
