using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;


using SixLabors.Primitives;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors;

namespace Epsylon.ImageSharp.Procedural
{
    using V2 = System.Numerics.Vector2;
    using V3 = System.Numerics.Vector3;
    using V4 = System.Numerics.Vector4;

    public static class _PublicExtensions
    {
        #region intrinsics

        internal static bool _IsReal(this float v)
        {
            return !(float.IsNaN(v) | float.IsInfinity(v));
        }

        internal static bool _IsInRange(this float v, float min, float max)
        {
            return (v >= min) & (v <= max);
        }

        /// <summary>
        /// rounds a floating point value, where values between 0 and 0.5 inclusive ar rounded to the lowest integer
        /// </summary>
        /// <param name="v">a floating point value</param>
        /// <returns>a rounded value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float _RoundDown(this float v) { return (float)Math.Ceiling(v - 0.5f); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int Clamp(this int v, int min, int max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float Clamp(this float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double Clamp(this double v, double min, double max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        internal static Boolean Bit(this Int32 value, int idx) => ((value >> idx) & 1) == 1;

        internal static Int32 WithBit(this Int32 value, int idx, bool bit) => bit ? (value | (1 << idx)) : (value & ~(1 << idx));

        /// <summary>
        /// Ensures the output values is in the range of 0 &lt;= value &lt; <paramref name="count"/>, in a round about manner
        /// </summary>
        /// <param name="idx">Any value, positive or negative</param>
        /// <param name="count">A positive value higher than 0</param>
        /// <returns>An integer within the range of inclusive 0 to exclusive <paramref name="count"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RoundAbout(this int idx, int count)
        {
            System.Diagnostics.Debug.Assert(count > 0, $"{nameof(count)} must be larger than zero");

            return idx >= 0 ? idx % count : count - ((-idx - 1) % count) - 1;
        }

        /// <summary>
        /// Utility class to get an initialization seed from any string
        /// </summary>
        /// <param name="value">any string</param>
        /// <returns>An integer value</returns>
        public static int GetRandomSeedHash(this string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return (int)DateTime.Now.Ticks;

            // https://referencesource.microsoft.com/#mscorlib/system/string.cs,854

            int hash1 = 5381;
            int hash2 = hash1;

            for (int i = 0; i < value.Length; ++i)
            {
                var c = (int)value[i];

                if (!i.Bit(0)) hash1 = ((hash1 << 5) + hash1) ^ c;
                else hash2 = ((hash2 << 5) + hash2) ^ c;
            }

            return hash1 + (hash2 * 1566083941);
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RoundedTextureCoordinate(this float v)
        {
            System.Diagnostics.Debug.Assert(v._IsReal());

            // standard Math.Floor rounds to the integer closest to ZERO, but for texture sampling, we need to round to the "immediate left" integer (1.3 = 1;  -2.3 = -3)

            int i = (int)v; return (float)i <= v ? i : i - 1;
        }

        #endregion

        #region vectors

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool _IsReal(this V2 v) { return v.X._IsReal() & v.Y._IsReal(); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool _IsReal(this V3 v) { return v.X._IsReal() & v.Y._IsReal() & v.Z._IsReal(); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool _IsReal(this V4 v) { return v.X._IsReal() & v.Y._IsReal() & v.Z._IsReal() & v.W._IsReal(); }        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static V2 RoundDown(this V2 source) { return new V2(source.X._RoundDown(), source.Y._RoundDown()); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static V4 Premultiply(this V4 source)
        {
            float w = source.W;
            var premultiplied = source * w;
            return premultiplied.WithW(w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static V4 UnPremultiply(this V4 source)
        {
            float w = source.W;
            var unpremultiplied = source / w;
            return unpremultiplied.WithW(w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static V4 ApplyAsNormalBlend(this V4 dest, V4 source)
        {
            source = source.Premultiply();
            dest = V4.Lerp(dest, source, source.W);
            return dest.UnPremultiply();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static V4 Clamp(this V4 v, V4 min, V4 max) { return V4.Min(max, V4.Max(min, v)); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static V4 Saturate(this V4 v) { return v.Clamp(V4.Zero, V4.One); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static V4 WithX(this V4 v, float x) { v.X = x; return v; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static V4 WithY(this V4 v, float y) { v.Y = y; return v; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static V4 WithZ(this V4 v, float z) { v.Z = z; return v; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static V4 WithW(this V4 v, float w) { v.W = w; return v; }

        /// <summary>
        /// Vector4 LERP method that interprets the W element as Alpha transparency, and uses it to weight the LERP result.
        /// </summary>
        /// <param name="a">first color value</param>
        /// <param name="b">second color value</param>
        /// <param name="amountOfB">weight of b</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V4 LerpRGBA(this V4 a, V4 b, float amountOfB) // LerpRGBA
        {
            System.Diagnostics.Debug.Assert(a._IsReal());
            System.Diagnostics.Debug.Assert(b._IsReal());
            System.Diagnostics.Debug.Assert(amountOfB._IsReal());
            System.Diagnostics.Debug.Assert(amountOfB._IsInRange(0, 1));

            var amountOfA = 1.0f - amountOfB;

            if (a.W == 0) return new V4(b.X, b.Y, b.Z, b.W * amountOfB);
            if (b.W == 0) return new V4(a.X, a.Y, a.Z, a.W * amountOfA);

            return a * amountOfA + b * amountOfB;
        }

        #endregion                

        #region SixLabors primitive helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Rgba32 WithAlpha(this Rgba32 color, int alpha)
        {
            color.A = (Byte)alpha.Clamp(0, 255);
            return color;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Rgba32 WithRed(this Rgba32 color, int red)
        {
            color.R = (Byte)red.Clamp(0, 255);
            return color;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Rgba32 WithGreen(this Rgba32 color, int green)
        {
            color.G = (Byte)green.Clamp(0, 255);
            return color;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Rgba32 WithBlue(this Rgba32 color, int blue)
        {
            color.B = (Byte)blue.Clamp(0, 255);
            return color;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(this Rectangle rect) { return rect.Width >= 0 && rect.Height >= 0; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(this RectangleF rect) { return rect.Width >= 0 && rect.Height >= 0; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInitialized(this Rectangle rect)
        {
            if (rect.X != int.MaxValue) return true;
            if (rect.Y != int.MaxValue) return true;
            if (rect.Width != int.MinValue) return true;
            if (rect.Height != int.MinValue) return true;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInitialized(this RectangleF rect)
        {
            if (rect.X != float.PositiveInfinity) return true;
            if (rect.Y != float.PositiveInfinity) return true;
            if (rect.Width != float.NegativeInfinity) return true;
            if (rect.Height != float.NegativeInfinity) return true;
            return false;
        }

        public static Rectangle UnionWith(this Rectangle a, Rectangle b)
        {
            var x = Math.Min(a.X, b.X);
            var y = Math.Min(a.Y, b.Y);
            var w = Math.Max(a.Right, b.Right) - x;
            var h = Math.Max(a.Bottom, b.Bottom) - y;

            return new Rectangle(x, y, w, h);
        }

        public static RectangleF UnionWith(this RectangleF a, RectangleF b)
        {
            var x = Math.Min(a.X, b.X);
            var y = Math.Min(a.Y, b.Y);
            var w = Math.Max(a.Right, b.Right) - x;
            var h = Math.Max(a.Bottom, b.Bottom) - y;

            return new RectangleF(x, y, w, h);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Area(this Size size) { return size.Width * size.Height; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Area(this Rectangle rect) { return rect.Size.Area(); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Area(this SizeF size) { return size.Width * size.Height; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Area(this RectangleF rect) { return rect.Size.Area(); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point TopLeft(this Rectangle rect) { return new Point(rect.Left, rect.Top); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point TopRight(this Rectangle rect) { return new Point(rect.Right, rect.Top); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point BottomLeft(this Rectangle rect) { return new Point(rect.Left, rect.Bottom); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point BottomRight(this Rectangle rect) { return new Point(rect.Right, rect.Bottom); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PointF TopLeft(this RectangleF rect) { return new PointF(rect.Left, rect.Top); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PointF TopRight(this RectangleF rect) { return new PointF(rect.Right, rect.Top); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PointF BottomLeft(this RectangleF rect) { return new PointF(rect.Left, rect.Bottom); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PointF BottomRight(this RectangleF rect) { return new PointF(rect.Right, rect.Bottom); }

        public static Point[] Get4Points(this Rectangle rect)
        {
            return new Point[]
            {
                rect.TopLeft(),
                rect.TopRight(),
                rect.BottomRight(),
                rect.BottomLeft()
            };
        }

        public static Point[] Get5Points(this Rectangle rect)
        {
            return new Point[]
            {
                rect.TopLeft(),
                rect.TopRight(),
                rect.BottomRight(),
                rect.BottomLeft(),
                rect.TopLeft()
            };
        }

        public static PointF[] Get4Points(this RectangleF rect)
        {
            return new PointF[]
            {
                rect.TopLeft(),
                rect.TopRight(),
                rect.BottomRight(),
                rect.BottomLeft()
            };
        }

        public static PointF[] Get5Points(this RectangleF rect)
        {
            return new PointF[]
            {
                rect.TopLeft(),
                rect.TopRight(),
                rect.BottomRight(),
                rect.BottomLeft(),
                rect.TopLeft()
            };
        }

        public static void ForEachPoint(this Size size, Action<Point> function)
        {
            var b = size.Height;
            var r = size.Width;

            for (int y = 0; y < b; ++y)
            {
                for (int x = 0; x < r; ++x)
                {
                    function(new Point(x, y));
                }
            }
        }

        public static void ForEachPoint(this Rectangle rect, Action<Point> function)
        {
            var b = rect.Bottom;
            var r = rect.Right;

            for(int y=rect.Top; y < b; ++y)
            {
                for (int x = rect.Left; x < r; ++x)
                {
                    function(new Point(x, y));
                }
            }
        }

        public static Rectangle OuterRound(this RectangleF rect)
        {
            return new Rectangle
                (
                (int)Math.Floor(rect.X),
                (int)Math.Floor(rect.Y),
                (int)Math.Ceiling(rect.Width),
                (int)Math.Ceiling(rect.Height)
                );
        }

        #endregion

        #region SixLabors images

        internal static void DrawPixel<TPixel>(this Image<TPixel> image, int x, int y, TPixel color, GraphicsOptions gfx) where TPixel : struct, IPixel<TPixel>
        {
            if (x < 0 || y < 0 || x >= image.Width || y >= image.Height) return;

            // this should be MUCH faster if we had access to PixelBlender<TPixel>            

            image.Mutate(dc => dc.Fill(gfx, color, new Rectangle(x, y, 1, 1)));
        }


        internal static Rectangle FitWithinImage<TPixel>(this Rectangle rect, Image<TPixel> image) where TPixel : struct, IPixel<TPixel>
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
        internal static TPixel GetAverageColor<TPixel>(this Image<TPixel> source, Rectangle sourceRectangle) where TPixel : struct, IPixel<TPixel>
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

            var r = w == 0 ? V4.Zero : new V4((float)(x / w), (float)(y / w), (float)(z / w), 1);

            var p = default(TPixel);
            p.PackFromVector4(r);

            return p;
        }

        #endregion

        #region complex extensions

        private static readonly V2 V2HALF = V2.One / 2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<int, int> GetFunction(this AddressMode mode, int size)
        {
            switch (mode)
            {
                case AddressMode.Clamp: return idx => idx.Clamp(0, size - 1);
                case AddressMode.Wrap: return idx => idx.RoundAbout(size);

                default: throw new NotImplementedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetFinalIndex(this AddressMode mode, int index, int size)
        {
            switch (mode)
            {
                case AddressMode.Clamp: return index.Clamp(0, size - 1);
                case AddressMode.Wrap: return index.RoundAbout(size);

                default: throw new NotImplementedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int _GetFinalIndex(int index, int size, AddressMode mode)
        {
            switch (mode)
            {
                case AddressMode.Clamp: return index.Clamp(0, size - 1);
                case AddressMode.Wrap: return index.RoundAbout(size);

                default: throw new NotImplementedException();
            }
        }

        public static TPixel GetSample<TPixel>(this Image<TPixel> image, V2 p, AddressMode u = AddressMode.Wrap, AddressMode v = AddressMode.Wrap) where TPixel : struct, IPixel<TPixel>
        {
            p -= V2HALF;

            int ix = p.X.RoundedTextureCoordinate();
            int iy = p.Y.RoundedTextureCoordinate();

            int w = image.Width;
            int h = image.Height;

            ix = _GetFinalIndex(ix, w, u);
            iy = _GetFinalIndex(iy, h, v);

            var jx = _GetFinalIndex(ix + 1, w, u);
            var jy = _GetFinalIndex(iy + 1, h, v);

            var A = image[ix, iy].ToVector4();
            var B = image[jx, iy].ToVector4();
            var C = image[ix, jy].ToVector4();
            var D = image[jx, jy].ToVector4();

            p.X -= ix;
            p.Y -= iy;

            A = A.LerpRGBA(B, p.X); // first row
            C = C.LerpRGBA(D, p.X); // second row
            A = A.LerpRGBA(C, p.Y); // column

            var r = default(TPixel);
            r.PackFromVector4(A);

            return r;
        }
        
        private sealed class _ActionProcessor<TPixel> : IImageProcessor<TPixel> where TPixel : struct, IPixel<TPixel>
        {
            public static _ActionProcessor<TPixel> Create(Action<Image<TPixel>> action) { return new _ActionProcessor<TPixel>(action); }

            private _ActionProcessor(Action<Image<TPixel>> action) { _Action = action; }

            private readonly Action<Image<TPixel>> _Action;

            public void Apply(Image<TPixel> source, Rectangle sourceRectangle)
            {
                _Action?.Invoke(source);
            }
        }

        public static IImageProcessingContext<TPixel> ApplyProcessor<TPixel>(this IImageProcessingContext<TPixel> source, Action<Image<TPixel>> action) where TPixel : struct, IPixel<TPixel>
        {
            var processor = _ActionProcessor<TPixel>.Create(action);

            return source.ApplyProcessor(processor);
        }

        

        

        public static Rectangle Bounds(this IBitmapSampler tex) { return new Rectangle(0, 0, tex.Width, tex.Height); }

        public static bool Contains(this IBitmapSampler texture, int x, int y) { return texture.Bounds().Contains(x, y); }

        public static bool Contains(this IBitmapSampler image, Point p) { return image.Contains(p.X, p.Y); }

        #endregion
    }
}
