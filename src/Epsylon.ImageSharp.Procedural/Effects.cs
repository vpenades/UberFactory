
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;

namespace Epsylon.ImageSharp.Procedural
{
    public enum BlurMode { Box, Gaussian }

    public static class Effects
    {
        public static IImageProcessingContext<TPixel> Blur<TPixel>(this IImageProcessingContext<TPixel> source, BlurMode mode, float radius) where TPixel : struct, IPixel<TPixel>
        {
            if (mode == BlurMode.Box) return source.BoxBlur((int)radius);
            if (mode == BlurMode.Gaussian) return source.GaussianBlur(radius);

            throw new NotImplementedException();
        }

        public static IImageProcessingContext<TPixel> Blur<TPixel>(this IImageProcessingContext<TPixel> source, Rectangle sourceRectangle, BlurMode mode, float radius) where TPixel : struct, IPixel<TPixel>
        {
            if (mode == BlurMode.Box) return source.BoxBlur((int)radius, sourceRectangle);
            if (mode == BlurMode.Gaussian) return source.GaussianBlur(radius, sourceRectangle);

            throw new NotImplementedException();
        }

        public static IImageProcessingContext<TPixel> OuterGlow<TPixel>(this IImageProcessingContext<TPixel> source, float radius) where TPixel : struct, IPixel<TPixel>
        {
            return source.Apply(img => _ApplyOuterGlow(img, radius));            
        }

        public static IImageProcessingContext<TPixel> EdgePadding<TPixel>(this IImageProcessingContext<TPixel> source, float minAlpha, int steps) where TPixel : struct, IPixel<TPixel>
        {
            return source.Apply(img => _ApplyEdgePadding(img, minAlpha, steps));
        }



        private static void _ApplyOuterGlow<TPixel>(Image<TPixel> source, float radius) where TPixel : struct, IPixel<TPixel>
        {
            // 1- Copies the original to a TOP clone
            // 2- blurs the original
            // 3- Blits the TOP clone over the original                                


            using (var top = source.Clone())
            {
                source.Mutate
                    (
                    dc =>
                    dc
                    .EdgePadding(0.1f, 1+ (int) radius)
                    .Blur(BlurMode.Gaussian, radius)
                    .DrawImage(top, 1, new Size(top.Width,top.Height), Point.Empty)
                    );
            }

            // Alternative Method:
            // 1- Copies the original to a GLOW clone
            // 2- blurs the GLOW clone
            // 3- substract original Alpha from GLOW clone
            // 4- apply GLOW clone on top
        }


        // this is typically called Flood-Filling or Edge-Padding, Dilate-Color
        // http://www.adriancourreges.com/blog/2017/05/09/beware-of-transparent-pixels/
        // http://wiki.polycount.com/wiki/Edge_padding
        // https://docs.unity3d.com/462/Documentation/Manual/HOWTO-alphamaps.html
        static void _ApplyEdgePadding<TPixel>(this Image<TPixel> source, float minAlpha, int steps) where TPixel : struct, IPixel<TPixel>
        {
            using (var mask = new Image<Alpha8>(source.Width,source.Height))
            {
                for (int y = 0; y < source.Height; ++y)
                {
                    for (int x = 0; x < source.Width; ++x)
                    {
                        var alpha = source[x, y].ToVector4().W;
                        mask[x, y] = alpha <= minAlpha ? new Alpha8(0) : new Alpha8(1);
                    }
                }

                for (int i=0; i < steps; ++i)
                {
                    source._DilateColor(mask);
                }                
            }
        }

        static void _DilateColor<TPixel>(this Image<TPixel> source, Image<Alpha8> mask) where TPixel : struct, IPixel<TPixel>
        {
            for(int y=0; y < source.Height; ++y)
            {
                for (int x = 0; x < source.Width; ++x)
                {
                    if (mask[x, y].PackedValue > 0) continue;

                    var curr = Vector4.Zero;

                    if (y > 0)
                    {
                        var m = mask[x, y - 1].PackedValue;
                        var s = source[x, y - 1].ToVector4();
                        s.W = 1;
                        if (m > 0) curr += s;
                    }

                    if (y < (source.Height - 1))
                    {
                        var m = mask[x, y + 1].PackedValue;
                        var s = source[x, y + 1].ToVector4();
                        s.W = 1;
                        if (m > 0) curr += s;
                    }

                    if (x > 0)
                    {
                        var m = mask[x-1, y].PackedValue;
                        var s = source[x-1, y].ToVector4();
                        s.W = 1;
                        if (m > 0) curr += s;
                    }
                    if (x < (source.Width - 1))
                    {
                        var m = mask[x + 1, y].PackedValue;
                        var s = source[x + 1, y].ToVector4();
                        s.W = 1;
                        if (m > 0) curr += s;
                    }

                    if (curr.W <= 0) continue;                    

                    curr.X /= curr.W;
                    curr.Y /= curr.W;
                    curr.Z /= curr.W;
                    curr.W = 0;

                    var c = default(TPixel);
                    c.PackFromVector4(curr);

                    source[x, y] = c;
                    mask[x, y] = new Alpha8(1);
                }            
            }
        }
    }
}
