
using System;
using System.Collections.Generic;
using System.Linq;
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

        public static IImageProcessingContext<TPixel> EdgePadding<TPixel>(this IImageProcessingContext<TPixel> source, float minAlpha) where TPixel : struct, IPixel<TPixel>
        {
            return source.Apply(img => _ApplyEdgePadding(img, minAlpha, int.MaxValue));
        }

        public static IImageProcessingContext<TPixel> SetAlphaMask<TPixel>(this IImageProcessingContext<TPixel> source, Image<Alpha8> mask, PixelBlenderMode mode) where TPixel : struct, IPixel<TPixel>
        {
            if (mask == null) return source;

            return source.Apply
                (
                img =>
                {
                    if (mask.Width == img.Width && mask.Height == img.Height) img.ToTexture()._ApplyAlphaMask(mask, mode);
                    else
                    {
                        using (var rmask = mask.Clone(dc => dc.Resize(img.Width, img.Height)))
                        {
                            img.ToTexture()._ApplyAlphaMask(rmask, mode);
                        }
                    }
                }
                );
        }


        private static void _ApplyAlphaMask(this ITexture source, Image<Alpha8> mask, PixelBlenderMode mode)
        {
            Func<float,float,float> alphaFunc = (a,b) => a * b;

            if (mode == PixelBlenderMode.Src) alphaFunc = (a, b) => a;
            if (mode == PixelBlenderMode.Dest) alphaFunc = (a, b) => b;
            if (mode == PixelBlenderMode.Add) alphaFunc = (a, b) => Math.Min(1,a+b);
            if (mode == PixelBlenderMode.Substract) alphaFunc = (a, b) => Math.Max(0, a - b);            

            var w = Math.Min(source.Width, mask.Width);
            var h = Math.Min(source.Height, mask.Height);

            for(int y=0; y < h; ++y)
            {
                for(int x=0; x < w; ++x)
                {
                    var texel = source[x, y];
                    var alpha = mask[x, y].ToVector4().W;
                    texel.W = alphaFunc(alpha, texel.W);

                    source[x, y] = texel;
                }
            }


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
                    // .EdgePadding(0.1f, 1+ (int) radius)
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
        static void _ApplyEdgePadding<TPixel>(this Image<TPixel> source, float minAlpha, int maxSteps) where TPixel : struct, IPixel<TPixel>
        {
            using (var mask = source.Clone() )
            {
                for (int i=0; i < maxSteps; ++i)
                {
                    var count = source.ToTexture()._DilateColor(mask.ToTexture());

                    if (count == 0) break;
                }                
            }
        }        

        static int _DilateColor(this ITexture target, ITexture mask, float alphaThreshold = 0)
        {
            int count = 0;

            var rows = new Dictionary<int, Vector4[]>();

            for (int y=0; y < mask.Height; ++y)
            {
                var row = rows[y] = new Vector4[mask.Width];

                for (int x = 0; x < mask.Width; ++x)
                {
                    row[x] = mask._GetDilatedTexel(x, y, alphaThreshold);
                }

                foreach(var k in rows.Keys.Where(item => (y-item) > 2).ToList())
                {
                    count += target._ApplyRow(k, rows[k], true);
                    mask._ApplyRow(k, rows[k], false);
                    rows.Remove(k);
                }
            }

            // apply remaining rows
            foreach (var k in rows.Keys.ToList())
            {
                count += target._ApplyRow(k, rows[k], true);
                mask._ApplyRow(k, rows[k], false);
                rows.Remove(k);
            }

            return count;
        }

        private static int _ApplyRow(this ITexture target,int y, Vector4[] row, bool ignoreAlpha)
        {
            int count = 0;

            for(int x=0; x < row.Length; ++x)
            {
                var c = row[x]; if (c.W == 0) continue;

                if (ignoreAlpha) c.W = 0;

                target[x, y] = c;

                ++count;
            }

            return count;
        }

        private static readonly Point[] _DilateKernel = new Point[]
        {
            new Point(-1,-1),new Point(0,-1),new Point(1,-1),
            new Point( 0,-1),                new Point(1, 0),
            new Point( 1,-1),new Point(0, 1),new Point(1, 1),
        };

        /// <summary>
        /// Gets a sampling of the 9 neighbour pixels
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="alphaThreshold"></param>
        /// <returns>
        /// If the central pixel is not transparent, returns the pixel itself, else it returns a sampling.
        /// </returns>
        static Vector4 _GetDilatedTexel(this ITexture texture, int x, int y, float alphaThreshold = 0)
        {
            var c = texture[x, y]; if (c.W > alphaThreshold) return Vector4.Zero;

            c = Vector4.Zero;

            for (int i = 0; i < _DilateKernel.Length; ++i)
            {
                var p = _DilateKernel[i];
                p.X += x;
                p.Y += y;
                
                var k = texture[p.X, p.Y];
                if (k.W > alphaThreshold) c += k.WithW(1);                
            }

            if (c.W > 0) { c /= c.W; c = c.Saturate(); }

            return c;
        }
    }
}
