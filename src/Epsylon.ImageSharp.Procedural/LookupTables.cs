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
        public static void FillPolarLookupTable(this Image<HalfVector2> target, Rectangle rect)
        {
            var c = new System.Numerics.Vector2((float)rect.Width / 2.0f, (float)rect.Height / 2.0f);

            for (int y = 0; y < rect.Height; ++y)
            {
                var dy = y + rect.Y;

                for (int x = 0; x < rect.Width; ++x)
                {
                    var dx = x + rect.X;

                    var p = _PixelEvaluator(x, y, c);

                    target[dx, dy] = new HalfVector2(p);
                }
            }
        }

        private static System.Numerics.Vector2 _PixelEvaluator(int y, int x, System.Numerics.Vector2 center)
        {
            var p = new System.Numerics.Vector2((float)y + 0.5f, (float)x + 0.5f) - center;            

            var angle = Math.Atan2(y, x);
            angle += Math.PI;
            angle /= Math.PI * 2;

            p /= center;

            var radius = p.Length();

            return new System.Numerics.Vector2((float)angle.Clamp(0, 1), (float)radius.Clamp(0, 1));            
        }

        public static void DrawImage<TPixel>(this Image<TPixel> target, Image<HalfVector2> targetLookup, Image<TPixel> source) where TPixel : struct, IPixel<TPixel>
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

                    sx = sx.Clamp(0, target.Width-1);
                    sy = sy.Clamp(0, target.Height-1);

                    target[dx, dy] = source[sx, sy];
                }
            }
        }


        public static IImageProcessingContext<TPixel> ApplyPolarDistort<TPixel>(this IImageProcessingContext<TPixel> dc) where TPixel : struct, IPixel<TPixel>
        {
            var lutable = new _ApplyTable<TPixel>();

            lutable._TableFactory = (w, h) => { var img = new Image<HalfVector2>(w, h); img.FillPolarLookupTable(new Rectangle(0, 0, w, h)); return img; };

            return dc.ApplyProcessor(lutable);
        }

        sealed class _ApplyTable<TPixel> : IImageProcessor<TPixel> where TPixel : struct, IPixel<TPixel>
        {
            public Func<int, int, Image<HalfVector2>> _TableFactory;

            public void Apply(Image<TPixel> source, Rectangle sourceRectangle)
            {
                var table = _TableFactory(source.Width, source.Height);

                using (var clone = source.Clone())
                {
                    source.DrawImage(table, clone);
                }
            }
        }

    }
}
