using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Epsylon.ImageSharp.Procedural
{
    /// <summary>
    /// Modified Perlin Noise to generate tileable values
    /// </summary>
    /// <see cref="http://flafla2.github.io/2014/08/09/perlinnoise.html"/>
    /// <seealso cref="https://gist.github.com/Flafla2"/>
    /// <remarks>
    /// Interesting alternatives:
    /// https://github.com/wwwtyro/space-3d/blob/gh-pages/src/glsl/classic-noise-4d.snip
    /// </remarks>
    class Perlin_Tileable
    {
        #region lifecycle

        public Perlin_Tileable(int randomSeed, int repeat = -1)
        {
            _Repeat = repeat;

            var permutations = Enumerable.Range(0, 256).ToList();

            var randomizer = new Random(randomSeed);

            for(int i=0; i < 256; ++i)
            {
                var idx = randomizer.Next(permutations.Count);
                var value = permutations[idx];

                permutations[idx] = permutations[permutations.Count - 1];
                permutations.RemoveAt(permutations.Count - 1);

                _Permutation2[i] = value;
                _Permutation2[i + 256] = value;                
            }
        }        

        #endregion

        #region data

        private readonly int _Repeat;        

        private readonly int[] _Permutation2 = new int[512]; // Doubled permutation to avoid overflow

        #endregion

        #region API

        public double OctavePerlin(double x, double y, double z, int octaves, double persistence)
        {
            double total = 0;
            double frequency = 1;
            double amplitude = 1;
            double maxValue = 0;            // Used for normalizing result to 0.0 - 1.0
            for (int i = 0; i < octaves; i++)
            {
                total += Perlin(x * frequency, y * frequency, z * frequency) * amplitude;

                maxValue += amplitude;

                amplitude *= persistence;
                frequency *= 2;
            }

            return total / maxValue;
        }

        public double Perlin(double x, double y, double z)
        {
            if (_Repeat > 0)
            {                                   // If we have any repeat on, change the coordinates to their "local" repetitions
                x = x % _Repeat;
                y = y % _Repeat;
                z = z % _Repeat;
            }

            int xi = (int)x & 255;                              // Calculate the "unit cube" that the point asked will be located in
            int yi = (int)y & 255;                              // The left bound is ( |_x_|,|_y_|,|_z_| ) and the right bound is that
            int zi = (int)z & 255;                              // plus 1.  Next we calculate the location (from 0.0 to 1.0) in that cube.
            double xf = x - (int)x;                             // We also fade the location to smooth the result.
            double yf = y - (int)y;


            double zf = z - (int)z;
            double u = _Fade(xf);
            double v = _Fade(yf);
            double w = _Fade(zf);

            int aaa, aba, aab, abb, baa, bba, bab, bbb;            

            aaa = _Permutation2[_Permutation2[_Permutation2[           xi ] +            yi ] +            zi ];
            aba = _Permutation2[_Permutation2[_Permutation2[           xi ] + _Increment(yi)] +            zi ];
            aab = _Permutation2[_Permutation2[_Permutation2[           xi ] +            yi ] + _Increment(zi)];
            abb = _Permutation2[_Permutation2[_Permutation2[           xi ] + _Increment(yi)] + _Increment(zi)];
            baa = _Permutation2[_Permutation2[_Permutation2[_Increment(xi)] +            yi ] +            zi ];
            bba = _Permutation2[_Permutation2[_Permutation2[_Increment(xi)] + _Increment(yi)] +            zi ];
            bab = _Permutation2[_Permutation2[_Permutation2[_Increment(xi)] +            yi ] + _Increment(zi)];
            bbb = _Permutation2[_Permutation2[_Permutation2[_Increment(xi)] + _Increment(yi)] + _Increment(zi)];

            double x1, x2, y1, y2;

            x1 = _Lerp(_Grad(aaa, xf, yf, zf),                // The gradient function calculates the dot product between a pseudorandom
                       _Grad(baa, xf - 1, yf, zf),              // gradient vector and the vector from the input coordinate to the 8
                        u);                                     // surrounding points in its unit cube.
            x2 = _Lerp(_Grad(aba, xf, yf - 1, zf),                // This is all then lerped together as a sort of weighted average based on the faded (u,v,w)
                       _Grad(bba, xf - 1, yf - 1, zf),              // values we made earlier.
                        u);
            y1 = _Lerp(x1, x2, v);

            x1 = _Lerp(_Grad(aab, xf, yf, zf - 1),
                       _Grad(bab, xf - 1, yf, zf - 1),
                        u);
            x2 = _Lerp(_Grad(abb, xf, yf - 1, zf - 1),
                       _Grad(bbb, xf - 1, yf - 1, zf - 1),
                        u);
            y2 = _Lerp(x1, x2, v);

            return (_Lerp(y1, y2, w) + 1) / 2;                       // For convenience we bound it to 0 - 1 (theoretical min/max before is -1 - 1)
        }

        private int _Increment(int num)
        {
            num++;

            if (_Repeat > 0) num %= _Repeat;

            return num;
        }

        private static double _Grad(int hash, double x, double y, double z)
        {
            int h = hash & 15;                                  // Take the hashed value and take the first 4 bits of it (15 == 0b1111)
            double u = h < 8 /* 0b1000 */ ? x : y;              // If the most significant bit (MSB) of the hash is 0 then set u = x.  Otherwise y.

            double v;                                           // In Ken Perlin's original implementation this was another conditional operator (?:).  I
                                                                // expanded it for readability.

            if (h < 4 /* 0b0100 */)                             // If the first and second significant bits are 0 set v = y
                v = y;
            else if (h == 12 /* 0b1100 */ || h == 14 /* 0b1110*/)// If the first and second significant bits are 1 set v = x
                v = x;
            else                                                // If the first and second significant bits are not equal (0/1, 1/0) set v = z
                v = z;

            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v); // Use the last 2 bits to decide if u and v are positive or negative.  Then return their addition.
        }

        private static double _Fade(double t)
        {
            // Fade function as defined by Ken Perlin.  This eases coordinate values
            // so that they will "ease" towards integral values.  This ends up smoothing
            // the final output.
            return t * t * t * (t * (t * 6 - 15) + 10);         // 6t^5 - 15t^4 + 10t^3
        }

        private static double _Lerp(double a, double b, double x)
        {
            return a + x * (b - a);
        }

        #endregion
    }


}
