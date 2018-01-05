using System;
using System.Collections.Generic;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;

namespace Epsylon.ImageSharp.Procedural
{
    public static class NoiseFactory
    {
        // https://en.wikipedia.org/wiki/Gaussian_noise

        public static IImageProcessingContext<TPixel> FillPerlinNoise<TPixel>(this IImageProcessingContext<TPixel> source, float scale=16, int repeat = 0, int octaves = 8, double persistence = 0.1f, int randomSeed = 177) where TPixel : struct, IPixel<TPixel>
        {
            var perlin = new _PerlinProcessor<TPixel>(scale, randomSeed, repeat,octaves,persistence);

            return source.ApplyProcessor(perlin);         
        }

        sealed class _PerlinProcessor<TPixel> : IImageProcessor<TPixel> where TPixel : struct, IPixel<TPixel>
        {
            public _PerlinProcessor(float scale, int randomSeed, int repeat, int octaves, double persistence)
            {
                _Scale = scale;
                _PerlinGenerator = new Perlin_Tileable(randomSeed, repeat);
                _Octaves = octaves;
                _Persistence = persistence;
            }

            private readonly float _Scale;
            private readonly int _Octaves;
            private readonly double _Persistence;

            private readonly Perlin_Tileable _PerlinGenerator;

            public void Apply(Image<TPixel> source, Rectangle sourceRectangle)
            {
                var noise = new float[sourceRectangle.Width * sourceRectangle.Height];

                float min = float.MaxValue;
                float max = float.MinValue;

                for (int y = 0; y < sourceRectangle.Height; ++y)
                {
                    for (int x = 0; x < sourceRectangle.Width; ++x)
                    {
                        var xx = (float)x / _Scale;
                        var yy = (float)y / _Scale;

                        var p = (float)_PerlinGenerator.OctavePerlin(xx, yy, 0, _Octaves, _Persistence);

                        noise[y * sourceRectangle.Width + x] = p;

                        if (min > p) min = p;
                        if (max < p) max = p;
                    }
                }

                float invmaxrange = 1.0f / (max - min);

                for (int y = 0; y < sourceRectangle.Height; ++y)
                {
                    var value = default(TPixel);

                    for (int x = 0; x < sourceRectangle.Width; ++x)
                    {
                        var p = noise[y * sourceRectangle.Width + x];

                        p = (p - min) * invmaxrange;

                        p = p.Clamp(0, 1);

                        var v = new System.Numerics.Vector4(p, p, p, 1);

                        value.PackFromVector4(v);

                        source[x, y] = value;
                    }
                }
            }
        }
    }
        



}
