using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;

namespace Epsylon.ImageSharp.Procedural
{
    using V2 = System.Numerics.Vector2;
    using V3 = System.Numerics.Vector3;
    using V4 = System.Numerics.Vector4;    

    static class _PrivateExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V4 Premultiply(this V4 source)
        {
            float w = source.W;
            var premultiplied = source * w;
            premultiplied.W = w;
            return premultiplied;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V4 UnPremultiply(this V4 source)
        {
            float w = source.W;
            var unpremultiplied = source / w;
            unpremultiplied.W = w;
            return unpremultiplied;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V4 ApplyAsNormal(this V4 dest, V4 source)
        {
            source = source.Premultiply();

            dest = V4.Lerp(dest, source, source.W);

            return dest.UnPremultiply();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Clamp(this int v, int min, int max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(this float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp(this double v, double min, double max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V4 Clamp(this V4 v, V4 min, V4 max)
        {
            v = V4.Min(max, v);
            v = V4.Max(min, v);
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V4 Saturate(this V4 v)
        {
            return v.Clamp(V4.Zero, V4.One);
        }        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V4 WithX(this V4 v, float x) { v.X = x; return v; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V4 WithY(this V4 v, float y) { v.Y = y; return v; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V4 WithZ(this V4 v, float z) { v.Z = z; return v; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V4 WithW(this V4 v, float w) { v.W = w; return v; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rgba32 WithAlpha(this Rgba32 color, int alpha)
        {
            alpha = Math.Min(alpha, 255);
            alpha = Math.Max(alpha, 0);

            color.A = (Byte)alpha;

            return color;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rgba32 WithRed(this Rgba32 color, int red)
        {
            red = Math.Min(red, 255);
            red = Math.Max(red, 0);

            color.R = (Byte)red;

            return color;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rgba32 WithGreen(this Rgba32 color, int green)
        {
            green = Math.Min(green, 255);
            green = Math.Max(green, 0);

            color.G = (Byte)green;

            return color;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rgba32 WithBlue(this Rgba32 color, int blue)
        {
            blue = Math.Min(blue, 255);
            blue = Math.Max(blue, 0);

            color.B = (Byte)blue;

            return color;
        }

        
        public static void DrawPixel<TPixel>(this Image<TPixel> image, int x, int y, TPixel color, GraphicsOptions gfx) where TPixel : struct, IPixel<TPixel>
        {
            if (x < 0 || y < 0 || x >= image.Width || y >= image.Height) return;

            // this should be MUCH faster if we had access to PixelBlender<TPixel>

            image.Mutate(dc => dc.Fill(color, new Rectangle(x, y, 1, 1), gfx));
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

            var ccc = V4.Zero;
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
