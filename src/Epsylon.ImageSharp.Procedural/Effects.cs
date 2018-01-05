
using System;
using System.Collections.Generic;
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
            var perlin = new _OuterGlow<TPixel>(radius);

            return source.ApplyProcessor(perlin);
        }

        sealed class _OuterGlow<TPixel> : IImageProcessor<TPixel> where TPixel : struct, IPixel<TPixel>
        {
            public _OuterGlow(float radius)
            {
                _Radius = radius;                
            }

            private readonly float _Radius;

            public void Apply(Image<TPixel> source, Rectangle sourceRectangle)
            {
                // 1- Copies the original to a TOP clone
                // 2- blurs the original
                // 3- Blits the TOP clone over the original

                using (var top = source.Clone(dc => dc.Crop(sourceRectangle)))
                {
                    source.Mutate
                        (
                        dc =>
                        dc                        

                        // .BackgroundColor, no Alpha || bleed color to transparent area
                        .Blur(BlurMode.Gaussian, _Radius)
                        .DrawImage(top, 1, sourceRectangle.Size, sourceRectangle.Location)
                        );
                }

                // Alternative Method:
                // 1- Copies the original to a GLOW clone
                // 2- blurs the GLOW clone
                // 3- substract original Alpha from GLOW clone
                // 4- apply GLOW clone on top
            }
        }
    }
}
