using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;

namespace Epsylon.ImageSharp.Procedural
{
    public static class NoiseFactory
    {
        public static IImageProcessingContext<HalfSingle> FillRandomNoise(this IImageProcessingContext<HalfSingle> source, int blurRadius = 0, int randomSeed = 177)
        {
            return source.Apply(image => image._FillRandomNoise(blurRadius,randomSeed));
        }

        public static IImageProcessingContext<HalfSingle> FillPerlinNoise(this IImageProcessingContext<HalfSingle> source, float scale = 16, int repeat = 0, int octaves = 8, double persistence = 0.1f, int randomSeed = 177)
        {
            return source.Apply(image => image._FillPerlinNoise(scale, repeat, octaves, persistence, randomSeed));
        }

        private static void _FillRandomNoise(this Image<HalfSingle> image, int blurRadius = 0, int randomSeed = 177)
        {
            var generator = new Random(randomSeed);

            for (int y = 0; y < image.Height; ++y)
            {
                for (int x = 0; x < image.Width; ++x)
                {
                    var p = (float)generator.NextDouble();

                    var pp = new HalfSingle(p);

                    image[x, y] = pp;
                }
            }

            if (blurRadius > 0)
            {
                image.Mutate(dc => dc.BoxBlur(blurRadius));
                image._MutateAutoLevels();
            }            
        }

        private static void _FillPerlinNoise(this Image<HalfSingle> image, float scale = 16, int repeat = 0, int octaves = 8, double persistence = 0.1f, int randomSeed = 177)
        {
            var generator = new Perlin_Tileable(randomSeed, repeat);

            for (int y = 0; y < image.Height; ++y)
            {
                for (int x = 0; x < image.Width; ++x)
                {
                    var xx = (float)x / scale;
                    var yy = (float)y / scale;

                    var p = (float)generator.OctavePerlin(xx, yy, 0, octaves, persistence);

                    var pp = new HalfSingle(p);

                    image[x,y] = pp;                    
                }
            }

            image._MutateAutoLevels();            
        }        
        
        private static void _MutateAutoLevels(this Image<HalfSingle> source)
        {
            source._MutateAutoLevels(new Rectangle(Point.Empty, new Size(source.Width, source.Height)));
        }

        private static void _MutateAutoLevels(this Image<HalfSingle> source, Rectangle sourceRectangle)
        {
            // stage 1: gather range

            float min = float.MaxValue;
            float max = float.MinValue;

            for (int y = sourceRectangle.Top; y < sourceRectangle.Bottom; ++y)
            {
                for (int x = sourceRectangle.Left; x < sourceRectangle.Right; ++x)
                {
                    var p = source[x, y].ToSingle();

                    if (min > p) min = p;
                    if (max < p) max = p;
                }
            }

            // stage 2: apply normalization

            var range = max - min; if (range <= float.Epsilon) return;

            float invmaxrange = 1.0f / range;

            for (int y = sourceRectangle.Top; y < sourceRectangle.Bottom; ++y)
            {
                for (int x = sourceRectangle.Left; x < sourceRectangle.Right; ++x)
                {
                    var p = source[x, y].ToSingle();

                    p = (p - min) * invmaxrange;
                    p = p.Clamp(0, 1);

                    source[x, y] = new HalfSingle(p);
                }
            }
        }

        public static Image<Rgba32> CloneWithLookupTable(this Image<HalfSingle> source, Rgba32[] gradient)
        {
            if (gradient == null) gradient = new Rgba32[] { Rgba32.Black, Rgba32.White };

            var vgradient = gradient.Select(item => item.ToVector4()).ToArray();

            var target = new Image<Rgba32>(source.Width, source.Height);

            for(int y=0; y < target.Height; ++y)
            {
                for(int x=0; x < target.Width; ++x)
                {
                    var z = source[x, y].ToSingle().Clamp(0,1);

                    z *= (vgradient.Length - 1);

                    var lowerIdx = (int)(z); if (lowerIdx >= vgradient.Length) lowerIdx = vgradient.Length - 1;
                    var upperIdx = (lowerIdx + 1); if (upperIdx >= vgradient.Length) upperIdx = vgradient.Length - 1;

                    z -= (int)z;

                    var c = System.Numerics.Vector4.Lerp(vgradient[lowerIdx], vgradient[upperIdx], z);

                    target[x, y] = new Rgba32(c);
                }
            }

            return target;
        }
    }
        



}
