using System;
using System.Collections.Generic;
using System.Text;

using SixLabors.ImageSharp;

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
    }
}
