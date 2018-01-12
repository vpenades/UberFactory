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
            float cx = (float)rect.Width / 2.0f;
            float cy = (float)rect.Height / 2.0f;

            for (int y=0; y < rect.Height; ++y)
            {
                var dy = y + rect.Y;

                float yy = (float)y + 0.5f - cy;

                for (int x=0; x < rect.Width; ++x)
                {
                    var dx = x + rect.X;

                    float xx = (float)x + 0.5f - cx;                    

                    var angle = Math.Atan2(y, x);
                    angle += Math.PI;
                    angle /= Math.PI * 2;

                    var xxx = xx / cx;
                    var yyy = yy / cy;
                    var radius = Math.Sqrt(xxx * xxx + yyy * yyy);

                    target[dx, dy] = new HalfVector2
                        (
                        (float)angle.Clamp(0,1),
                        (float)radius.Clamp(0,1)
                        );                    
                }
            }
        }

        public static void DrawImage<TPixel>(this Image<TPixel> target, Image<HalfVector2> targetLookup, Image<TPixel> source) where TPixel:struct , IPixel<TPixel>
        {
            var sourceScale = new System.Numerics.Vector2(source.Width, source.Height);

            for (int dy=0; dy < target.Height; ++dy)
            {
                for(int dx=0; dx < target.Width; ++dx)
                {
                    var mapper = targetLookup[dx, dy].ToVector2();

                    mapper *= sourceScale;

                    int sx = (int)mapper.X;
                    int sy = (int)mapper.Y;

                    target[dx, dy] = source[sx, sy];
                }
            }            
        }
    }
}
