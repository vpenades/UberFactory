using System;
using System.Collections.Generic;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Brushes;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;

namespace Epsylon.ImageSharp.Procedural
{
    public static class LookupTables
    {
        public static IImageProcessingContext<TPixel> ApplyPolarDistort<TPixel>(this IImageProcessingContext<TPixel> dc) where TPixel : struct, IPixel<TPixel>
        {
            return dc.Apply(image => image._DistortImage() );
        }

        private static void _FillPolarLookupTable(this Image<HalfVector2> target)
        {
            var c = new System.Numerics.Vector2((float)target.Width / 2.0f, (float)target.Height / 2.0f);

            for (int y = 0; y < target.Height; ++y)
            {
                for (int x = 0; x < target.Width; ++x)
                {
                    var p = _PixelEvaluator(x, y, c);

                    target[x, y] = new HalfVector2(p);
                }
            }
        }

        private static System.Numerics.Vector2 _PixelEvaluator(int x, int y, System.Numerics.Vector2 center)
        {
            var p = new System.Numerics.Vector2((float)x + 0.5f, (float)y + 0.5f) - center;            

            var angle = -Math.Atan2(p.X, p.Y);
            angle += Math.PI;
            angle /= Math.PI * 2;

            p /= center;

            var radius = p.Length();

            return new System.Numerics.Vector2((float)angle.Clamp(0, 1), 1- (float)radius.Clamp(0, 1));            
        }

        private static void _DistortImage<TPixel>(this Image<TPixel> target, Image<HalfVector2> targetLookup) where TPixel : struct, IPixel<TPixel>
        {
            using (var source = target.Clone())
            {
                var sourceScale = new System.Numerics.Vector2(source.Width, source.Height);

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



        private static void _DistortImage<TPixel>(this Image<TPixel> target) where TPixel : struct, IPixel<TPixel>
        {
            using (var source = target.Clone())
            {
                var sampler = SamplerFactory
                    .CreateSampler(source, SamplerAddressMode.Wrap, SamplerAddressMode.Clamp)
                    .ToPolarTransform()
                    .ToPointSampler(source.Width,source.Height);

                var tl = Point.Empty;
                var tr = Point.Empty;
                var bl = Point.Empty;
                var br = Point.Empty;

                for (int dy = 0; dy < target.Height; ++dy)
                {
                    tl.Y = tr.Y = dy;
                    bl.Y = br.Y = dy + 1;

                    for (int dx = 0; dx < target.Width; ++dx)
                    {
                        tl.X = bl.X = dx;
                        tr.X = br.X = dx + 1;                        

                        target[dx, dy] = sampler.GetSample(tl, tr, br, bl);
                    }
                }
            }
        }

    }
}
