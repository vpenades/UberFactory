using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;

namespace Epsylon.ImageSharp.Procedural
{
    public static class _PublicExtensions
    {
        public static T GetValue<T>(this IList<SixLabors.ImageSharp.MetaData.ImageProperty> properties, string key, T defval) where T : IConvertible
        {
            var p = properties.FirstOrDefault(item => item.Name == key);

            return p == null ? defval : (T)System.Convert.ChangeType(p.Value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
        }

        public static void SetValue<T>(this IList<SixLabors.ImageSharp.MetaData.ImageProperty> properties, string key, T val) where T : IConvertible
        {
            var idx = -1;

            for (int i = 0; i < properties.Count; ++i)
            {
                if (properties[i].Name == key)
                {
                    idx = i; break;
                }
            }

            var p = new SixLabors.ImageSharp.MetaData.ImageProperty(key, val.ToString(System.Globalization.CultureInfo.InvariantCulture));

            if (idx < 0) properties.Add(p);
            else properties[idx] = p;
        }

        public static Point GetInternalPixelOffset<TPixel>(this Image<TPixel> image) where TPixel : struct, IPixel<TPixel>
        {
            var x = image.MetaData.Properties.GetValue<int>("InternalPixelOffsetX", 0);
            var y = image.MetaData.Properties.GetValue<int>("InternalPixelOffsetY", 0);

            return new Point(x, y);
        }

        public static void SetInternalPixelOffset<TPixel>(this Image<TPixel> image, int x, int y) where TPixel : struct, IPixel<TPixel>
        {
            image.SetInternalPixelOffset(new Point(x, y));
        }

        public static void SetInternalPixelOffset<TPixel>(this Image<TPixel> image, Point offset) where TPixel : struct, IPixel<TPixel>
        {
            image.MetaData.Properties.SetValue<int>("InternalPixelOffsetX", offset.X);
            image.MetaData.Properties.SetValue<int>("InternalPixelOffsetY", offset.Y);
        }
    }

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
