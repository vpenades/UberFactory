using System;
using System.Collections.Generic;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;

namespace Epsylon.ImageSharp.Procedural
{
    

    static class _PrivateExtensions
    {
        public static float Clamp(this float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        public static double Clamp(this double v, double min, double max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        public static Rgba32 WithAlpha(this Rgba32 color, int alpha)
        {
            alpha = Math.Min(alpha, 255);
            alpha = Math.Max(alpha, 0);

            color.A = (Byte)alpha;

            return color;
        }

        public static Rgba32 WithRed(this Rgba32 color, int red)
        {
            red = Math.Min(red, 255);
            red = Math.Max(red, 0);

            color.R = (Byte)red;

            return color;
        }

        public static Rgba32 WithGreen(this Rgba32 color, int green)
        {
            green = Math.Min(green, 255);
            green = Math.Max(green, 0);

            color.G = (Byte)green;

            return color;
        }

        public static Rgba32 WithBlue(this Rgba32 color, int blue)
        {
            blue = Math.Min(blue, 255);
            blue = Math.Max(blue, 0);

            color.B = (Byte)blue;

            return color;
        }

        public static Rectangle FitWithinImage<TPixel>(this Rectangle rect, Image<TPixel> image) where TPixel : struct, IPixel<TPixel>
        {
            if (rect.X < 0) { rect.Width += rect.X; rect.X = 0; }
            if (rect.Y < 0) { rect.Height += rect.Y; rect.Y = 0; }

            if (rect.Right > image.Width) { rect.Width -= rect.Right - image.Width; }
            if (rect.Bottom > image.Height) { rect.Height -= rect.Bottom - image.Height; }

            return rect;
        }

        public static TPixel GetAverageColor<TPixel>(this Image<TPixel> source, Rectangle sourceRectangle) where TPixel : struct, IPixel<TPixel>
        {
            sourceRectangle.FitWithinImage(source);

            var ccc = System.Numerics.Vector4.Zero;
            float w = 0;

            for (int y=0; y < sourceRectangle.Height; ++y)
            {
                for (int x = 0; x < sourceRectangle.Height; ++x)
                {
                    var c = source[x + sourceRectangle.X, y + sourceRectangle.Y].ToVector4();

                    ccc += c;
                    w += c.W;                    
                }
            }

            ccc /= w;

            var p = default(TPixel);
            p.PackFromVector4(ccc);

            return p;
        }
    }
}
