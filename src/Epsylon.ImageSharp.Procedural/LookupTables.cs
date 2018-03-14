using System;
using System.Collections.Generic;
using System.Text;

using SixLabors.Primitives;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using V2 = System.Numerics.Vector2;

namespace Epsylon.ImageSharp.Procedural
{
    public static class LookupTables
    {
        public static IImageProcessingContext<TPixel> ApplyPolarDistort<TPixel>(this IImageProcessingContext<TPixel> dc, bool inverse) where TPixel : struct, IPixel<TPixel>
        {
            return dc.Apply(image => image._DistortImage(inverse) );
        }

        private static void _FillPolarLookupTable(this Image<HalfVector2> target)
        {
            var c = new V2((float)target.Width / 2.0f, (float)target.Height / 2.0f);

            target.Bounds().ForEachPoint
                (
                pc => {
                   var p = _PixelEvaluator(pc.X, pc.Y, c);
                   target[pc.X, pc.Y] = new HalfVector2(p);
                    }
                );            
        }

        private static V2 _PixelEvaluator(int x, int y, System.Numerics.Vector2 center)
        {
            var p = new V2((float)x + 0.5f, (float)y + 0.5f) - center;            

            var angle = -Math.Atan2(p.X, p.Y);
            angle += Math.PI;
            angle /= Math.PI * 2;

            p /= center;

            var radius = p.Length();

            return new V2((float)angle.Clamp(0, 1), 1- (float)radius.Clamp(0, 1));            
        }

        private static void _DistortImage<TPixel>(this Image<TPixel> target, Image<HalfVector2> targetLookup) where TPixel : struct, IPixel<TPixel>
        {
            using (var source = target.Clone())
            {
                var sourceScale = new V2(source.Width, source.Height);

                for (int dy = 0; dy < target.Height; ++dy)
                {
                    for (int dx = 0; dx < target.Width; ++dx)
                    {
                        var mapper = targetLookup[dx, dy].ToVector2();

                        mapper *= sourceScale;

                        int sx = (int)mapper.X;
                        int sy = (int)mapper.Y;

                        sx = sx.Clamp(0, target.Width - 1);
                        sy = sy.Clamp(0, target.Height - 1);

                        target[dx, dy] = source[sx, sy];
                    }
                }
            }
        }



        private static void _DistortImage<TPixel>(this Image<TPixel> target, bool inverse) where TPixel : struct, IPixel<TPixel>
        {
            using (var source = target.Clone())
            {
                var sampler = source
                    .ToPixelSampler(AddressMode.Wrap, AddressMode.Clamp)
                    .ToTextureSampler(false)
                    .ToPolarTransform(inverse);

                target.FitFill(sampler);                
            }
        }

    }
}
