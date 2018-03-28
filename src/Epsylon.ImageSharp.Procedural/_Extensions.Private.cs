using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Drawing;
using SixLabors.Primitives;

namespace Epsylon.ImageSharp.Procedural
{
    using V2 = System.Numerics.Vector2;
    using V3 = System.Numerics.Vector3;
    using V4 = System.Numerics.Vector4;    

    static class _PrivateExtensions
    {
        #region intrinsics

        // RoundUp => rounds to positive infinity
        // RoundDown => rounds to negative infinity
        // RoundAway => rounds away from zero
        // RoundZero => rounds to zero        

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

        public static Boolean Bit(this Int32 value, int idx) => ((value >> idx) & 1) == 1;

        public static Int32 WithBit(this Int32 value, int idx, bool bit) => bit ? (value | (1 << idx)) : (value & ~(1 << idx));

        #endregion

        #region vectors

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V2 Round(this V2 source)
        {
            return new V2((float)Math.Round(source.X), (float)Math.Round(source.Y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V4 Premultiply(this V4 source)
        {
            float w = source.W;
            var premultiplied = source * w;
            return premultiplied.WithW(w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V4 UnPremultiply(this V4 source)
        {
            float w = source.W;
            var unpremultiplied = source / w;
            return unpremultiplied.WithW(w);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V4 ApplyAsNormalBlend(this V4 dest, V4 source)
        {
            source = source.Premultiply();
            dest = V4.Lerp(dest, source, source.W);
            return dest.UnPremultiply();
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

        #endregion

        #region sixlabors primitives        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rgba32 WithAlpha(this Rgba32 color, int alpha)
        {
            color.A = (Byte)alpha.Clamp(0,255);
            return color;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rgba32 WithRed(this Rgba32 color, int red)
        {
            color.R = (Byte)red.Clamp(0,255);
            return color;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rgba32 WithGreen(this Rgba32 color, int green)
        {
            color.G = (Byte)green.Clamp(0,255);
            return color;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rgba32 WithBlue(this Rgba32 color, int blue)
        {
            color.B = (Byte)blue.Clamp(0,255);
            return color;
        }

        #endregion       

        #region sixlabors images

        public static void DrawPixel<TPixel>(this Image<TPixel> image, int x, int y, TPixel color, GraphicsOptions gfx) where TPixel : struct, IPixel<TPixel>
        {
            if (x < 0 || y < 0 || x >= image.Width || y >= image.Height) return;

            // this should be MUCH faster if we had access to PixelBlender<TPixel>

            image.Mutate(dc => dc.Fill(gfx, color, new Rectangle(x, y, 1, 1)));
        }


        public static Rectangle FitWithinImage<TPixel>(this Rectangle rect, Image<TPixel> image) where TPixel : struct, IPixel<TPixel>
        {
            if (rect.X < 0) { rect.Width += rect.X; rect.X = 0; }
            if (rect.Y < 0) { rect.Height += rect.Y; rect.Y = 0; }

            if (rect.Right > image.Width) { rect.Width -= rect.Right - image.Width; }
            if (rect.Bottom > image.Height) { rect.Height -= rect.Bottom - image.Height; }

            return rect;
        }

        /// <summary>
        /// Gets the alpha-weighted, average color of the pixels of the image contained in the given rectangle.
        /// </summary>
        /// <typeparam name="TPixel"></typeparam>
        /// <param name="source"></param>
        /// <param name="sourceRectangle"></param>
        /// <returns></returns>
        public static TPixel GetAverageColor<TPixel>(this Image<TPixel> source, Rectangle sourceRectangle) where TPixel : struct, IPixel<TPixel>
        {
            sourceRectangle = sourceRectangle.FitWithinImage(source);

            double x = 0;
            double y = 0;
            double z = 0;
            double w = 0;            

            sourceRectangle.ForEachPoint(
                pc =>
                {
                    var c = source[pc.X, pc.Y].ToVector4();

                    x += c.X;
                    y += c.Y;
                    z += c.Z;
                    w += c.W;
                }
                );

            var r = w == 0 ? V4.Zero : new V4( (float)(x / w), (float)(y / w), (float)(z / w), 1);            

            var p = default(TPixel);
            p.PackFromVector4(r);

            return p;
        }

        #endregion
    }
}
