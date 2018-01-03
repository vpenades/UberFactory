using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Epsylon.UberPlugin
{
    using Scalar = System.Single;
    using XY = System.Numerics.Vector2;
    using XYZ = System.Numerics.Vector3;
    using XYZW = System.Numerics.Vector4;

    public interface INoiseGenerator
    {
        // este API debe aceptar valores negativos, y devolver valores en el rango -1 a 1

        Scalar GetSample(Scalar x);
        Scalar GetSample(Scalar x, Scalar y);
        Scalar GetSample(Scalar x, Scalar y, Scalar z);
    }


    // http://libnoise.sourceforge.net/docs/classnoise_1_1module_1_1Perlin.html

    //https://gist.github.com/banksean/304522

    // issues: negative numbers !!
    // http://code.google.com/p/simplexnoise/issues/detail?id=1

    // http://playtechs.blogspot.com.es/2009/10/perlin-simplex-noise.html

    public static class PerlinSimplexNoise
    {
        public static float Clamped(this float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        #region data

        private static readonly Byte[,] _Simplex4 = new Byte[,]
        {
            {0,1,2,3},{0,1,3,2},{0,0,0,0},{0,2,3,1},{0,0,0,0},{0,0,0,0},{0,0,0,0},{1,2,3,0},
            {0,2,1,3},{0,0,0,0},{0,3,1,2},{0,3,2,1},{0,0,0,0},{0,0,0,0},{0,0,0,0},{1,3,2,0},
            {0,0,0,0},{0,0,0,0},{0,0,0,0},{0,0,0,0},{0,0,0,0},{0,0,0,0},{0,0,0,0},{0,0,0,0},
            {1,2,0,3},{0,0,0,0},{1,3,0,2},{0,0,0,0},{0,0,0,0},{0,0,0,0},{2,3,0,1},{2,3,1,0},
            {1,0,2,3},{1,0,3,2},{0,0,0,0},{0,0,0,0},{0,0,0,0},{2,0,3,1},{0,0,0,0},{2,1,3,0},
            {0,0,0,0},{0,0,0,0},{0,0,0,0},{0,0,0,0},{0,0,0,0},{0,0,0,0},{0,0,0,0},{0,0,0,0},
            {2,0,1,3},{0,0,0,0},{0,0,0,0},{0,0,0,0},{3,0,1,2},{3,0,2,1},{0,0,0,0},{3,1,2,0},
            {2,1,0,3},{0,0,0,0},{0,0,0,0},{0,0,0,0},{3,1,0,2},{0,0,0,0},{3,2,0,1},{3,2,1,0}
        };


        /*
         * Permutation table. This is just a random jumble of all numbers 0-255,
         * repeated twice to avoid wrapping the index at 255 for each lookup.
         * This needs to be exactly the same for all instances on all platforms,
         * so it's easiest to just keep it as static explicit data.
         * This also removes the need for any initialisation of this class.
         *
         * Note that making this an int[] instead of a char[] might make the
         * code run faster on platforms with a high penalty for unaligned single
         * byte addressing. Intel x86 is generally single-byte-friendly, but
         * some other CPUs are faster with 4-aligned reads.
         * However, a char[] is smaller, which avoids cache trashing, and that
         * is probably the most important aspect on most architectures.
         * This array is accessed a *lot* by the noise functions.
         * A vector-valued noise over 3D accesses it 96 times, and a
         * float-valued 4D noise 64 times. We want this to fit in the cache!
         */

        // 512 bytes
        private static readonly Byte[] _PermutationTable = new Byte[]
        {
            151,160,137, 91, 90, 15,131, 13,201, 95, 96, 53,194,233,  7,225,
            140, 36,103, 30, 69,142,  8, 99, 37,240, 21, 10, 23,190,  6,148,
            247,120,234, 75,  0, 26,197, 62, 94,252,219,203,117, 35, 11, 32,
            57 ,177, 33, 88,237,149, 56, 87,174, 20,125,136,171,168, 68,175,
            74 ,165, 71,134,139, 48, 27,166, 77,146,158,231, 83,111,229,122,
            60 ,211,133,230,220,105, 92, 41, 55, 46,245, 40,244,102,143, 54,
            65 , 25, 63,161,  1,216, 80, 73,209, 76,132,187,208, 89, 18,169,
            200,196,135,130,116,188,159, 86,164,100,109,198,173,186,  3, 64,
            52 ,217,226,250,124,123,  5,202, 38,147,118,126,255, 82, 85,212,
            207,206, 59,227, 47, 16, 58, 17,182,189, 28, 42,223,183,170,213,
            119,248,152,  2, 44,154,163, 70,221,153,101,155,167, 43,172,  9,
            129, 22, 39,253, 19, 98,108,110, 79,113,224,232,178,185,112,104,
            218,246, 97,228,251, 34,242,193,238,210,144, 12,191,179,162,241,
            81 , 51,145,235,249, 14,239,107, 49,192,214, 31,181,199,106,157,
            184, 84,204,176,115,121, 50, 45,127,  4,150,254,138,236,205, 93,
            222,114, 67, 29, 24, 72,243,141,128,195, 78, 66,215, 61,156,180,


            151,160,137, 91, 90, 15,131, 13,201, 95, 96, 53,194,233,  7,225,
            140, 36,103, 30, 69,142,  8, 99, 37,240, 21, 10, 23,190,  6,148,
            247,120,234, 75,  0, 26,197, 62, 94,252,219,203,117, 35, 11, 32,
            57 ,177, 33, 88,237,149, 56, 87,174, 20,125,136,171,168, 68,175,
            74 ,165, 71,134,139, 48, 27,166, 77,146,158,231, 83,111,229,122,
            60 ,211,133,230,220,105, 92, 41, 55, 46,245, 40,244,102,143, 54,
            65 , 25, 63,161,  1,216, 80, 73,209, 76,132,187,208, 89, 18,169,
            200,196,135,130,116,188,159, 86,164,100,109,198,173,186,  3, 64,
            52 ,217,226,250,124,123,  5,202, 38,147,118,126,255, 82, 85,212,
            207,206, 59,227, 47, 16, 58, 17,182,189, 28, 42,223,183,170,213,
            119,248,152,  2, 44,154,163, 70,221,153,101,155,167, 43,172,  9,
            129, 22, 39,253, 19, 98,108,110, 79,113,224,232,178,185,112,104,
            218,246, 97,228,251, 34,242,193,238,210,144, 12,191,179,162,241,
            81 , 51,145,235,249, 14,239,107, 49,192,214, 31,181,199,106,157,
            184, 84,204,176,115,121, 50, 45,127,  4,150,254,138,236,205, 93,
            222,114, 67, 29, 24, 72,243,141,128,195, 78, 66,215, 61,156,180
        };

        #endregion

        #region core

        private static int _ClampCircularIndex(int idx, int size)
        {
            System.Diagnostics.Debug.Assert(size > 0);
            return idx >= 0 ? idx % size : size - ((-idx - 1) % size) - 1;
        }

        // en la mayoria de ejemplos aparece como "x > 0" pero para hacer un floor no parece lo correcto
        private static int _FastFloor(float x) { return x >= 0 ? ((int)x) : (((int)x) - 1); }

        private static float _Grad(int hash, float x)
        {
            int h = hash & 15;
            float grad = 1.0f + (h & 7);   // Gradient value 1.0, 2.0, ..., 8.0
            if ((h & 8) != 0) grad = -grad;         // Set a random sign for the gradient
            return (grad * x);           // Multiply the gradient with the distance
        }

        private static float _Grad(int hash, float x, float y)
        {
            int h = hash & 7;      // Convert low 3 bits of hash code
            float u = h < 4 ? x : y;  // into 8 simple gradient directions,
            float v = h < 4 ? y : x;  // and compute the dot product with (x,y).
            return ((h & 1) != 0 ? -u : u) + ((h & 2) != 0 ? -2.0f * v : 2.0f * v);
        }

        private static float _Grad(int hash, float x, float y, float z)
        {
            int h = hash & 15;     // Convert low 4 bits of hash code into 12 simple
            float u = h < 8 ? x : y; // gradient directions, and compute dot product.
            float v = h < 4 ? y : h == 12 || h == 14 ? x : z; // Fix repeats at h = 12 to 15
            return ((h & 1) != 0 ? -u : u) + ((h & 2) != 0 ? -v : v);
        }

        private static float _Grad(int hash, float x, float y, float z, float t)
        {
            int h = hash & 31;      // Convert low 5 bits of hash code into 32 simple
            float u = h < 24 ? x : y; // gradient directions, and compute dot product.
            float v = h < 16 ? y : z;
            float w = h < 8 ? z : t;
            return ((h & 1) != 0 ? -u : u) + ((h & 2) != 0 ? -v : v) + ((h & 4) != 0 ? -w : w);
        }

        #endregion

        #region API

        public const float SCALE1D = 0.395f; // A factor of 0.395 would scale to fit exactly within [-1,1]
        public const float SCALE2D = 45;     // TODO: The scale factor is preliminary!
        public const float SCALE3D = 32;     // TODO: The scale factor is preliminary!
        public const float SCALE4D = 27;     // TODO: The scale factor is preliminary!

        // 1D, 2D, 3D and 4D float Simplex Perlin noise

        public static float Noise(float x)
        {
            System.Diagnostics.Debug.Assert(_PermutationTable.Length == 512);

            int i0 = _FastFloor(x);
            int i1 = i0 + 1;
            float x0 = x - i0;
            float x1 = x0 - 1.0f;

            float t0 = 1.0f - x0 * x0;
            //  if(t0 < 0.0f) t0 = 0.0f;
            t0 *= t0;
            float n0 = t0 * t0 * _Grad(_PermutationTable[_ClampCircularIndex(i0, 256)], x0);

            float t1 = 1.0f - x1 * x1;
            //  if(t1 < 0.0f) t1 = 0.0f;
            t1 *= t1;
            float n1 = t1 * t1 * _Grad(_PermutationTable[_ClampCircularIndex(i1, 256)], x1);

            // The maximum value of this noise is 8*(3/4)^4 = 2.53125
            // so we must Scale by 1.0f / 2.53125

            return SCALE1D * (n0 + n1);
        }

        public static float Noise(float x, float y)
        {
            //System.Diagnostics.Debug.Assert(x >= 0 && y >= 0, "doesn't work with negative values");

            const float F2 = 0.366025403f; // F2 = 0.5*(sqrt(3.0)-1.0)
            const float G2 = 0.211324865f; // G2 = (3.0-Math.sqrt(3.0))/6.0

            float n0, n1, n2; // Noise contributions from the three corners

            // Skew the input space to determine which simplex cell we're in
            float s = (x + y) * F2; // Hairy factor for 2D
            float xs = x + s;
            float ys = y + s;
            int i = _FastFloor(xs);
            int j = _FastFloor(ys);

            float t = (float)(i + j) * G2;
            float X0 = i - t; // Unskew the cell origin back to (x,y) space
            float Y0 = j - t;
            float x0 = x - X0; // The x,y distances from the cell origin
            float y0 = y - Y0;

            // For the 2D case, the simplex shape is an equilateral triangle.
            // Determine which simplex we are in.
            int i1, j1; // Offsets for second (middle) corner of simplex in (i,j) coords
            if (x0 > y0) { i1 = 1; j1 = 0; } // lower triangle, XY order: (0,0)->(1,0)->(1,1)
            else         { i1 = 0; j1 = 1; } // upper triangle, YX order: (0,0)->(0,1)->(1,1)

            // A step of (1,0) in (i,j) means a step of (1-c,-c) in (x,y), and
            // a step of (0,1) in (i,j) means a step of (-c,1-c) in (x,y), where
            // c = (3-sqrt(3))/6

            float x1 = x0 - i1 + G2; // Offsets for middle corner in (x,y) unskewed coords
            float y1 = y0 - j1 + G2;
            float x2 = x0 - 1.0f + 2.0f * G2; // Offsets for last corner in (x,y) unskewed coords
            float y2 = y0 - 1.0f + 2.0f * G2;

            // Wrap the integer indices at 256, to avoid indexing perm[] out of bounds
            //int ii = i % 256;
            //int jj = j % 256;

            // fix to allow input negative numbers
            int ii = _ClampCircularIndex(i, 256);
            int jj = _ClampCircularIndex(j, 256);

            // Calculate the contribution from the three corners
            float t0 = 0.5f - x0 * x0 - y0 * y0;
            if (t0 < 0.0f) n0 = 0.0f;
            else
            {
                t0 *= t0;
                n0 = t0 * t0 * _Grad(_PermutationTable[ii + _PermutationTable[jj]], x0, y0);
            }

            float t1 = 0.5f - x1 * x1 - y1 * y1;
            if (t1 < 0.0f) n1 = 0.0f;
            else
            {
                t1 *= t1;
                n1 = t1 * t1 * _Grad(_PermutationTable[ii + i1 + _PermutationTable[jj + j1]], x1, y1);
            }

            float t2 = 0.5f - x2 * x2 - y2 * y2;
            if (t2 < 0.0f) n2 = 0.0f;
            else
            {
                t2 *= t2;
                n2 = t2 * t2 * _Grad(_PermutationTable[ii + 1 + _PermutationTable[jj + 1]], x2, y2);
            }

            // Add contributions from each corner to get the final noise value.
            // The result is scaled to return values in the interval [-1,1].
            return SCALE2D * (n0 + n1 + n2);
        }

        public static float Noise(float x, float y, float z)
        {
            //System.Diagnostics.Debug.Assert(x >= 0 && y >= 0 && z >= 0, "doesn't work with negative values");

            // Simple skewing factors for the 3D case
            const float F3 = 0.333333333f;
            const float G3 = 0.166666667f;

            float n0, n1, n2, n3; // Noise contributions from the four corners

            // Skew the input space to determine which simplex cell we're in
            float s = (x + y + z) * F3; // Very nice and simple skew factor for 3D
            float xs = x + s;
            float ys = y + s;
            float zs = z + s;
            int i = _FastFloor(xs);
            int j = _FastFloor(ys);
            int k = _FastFloor(zs);            

            float t = (float)(i + j + k) * G3;
            float X0 = i - t; // Unskew the cell origin back to (x,y,z) space
            float Y0 = j - t;
            float Z0 = k - t;
            float x0 = x - X0; // The x,y,z distances from the cell origin
            float y0 = y - Y0;
            float z0 = z - Z0;

            // For the 3D case, the simplex shape is a slightly irregular tetrahedron.
            // Determine which simplex we are in.
            int i1, j1, k1; // Offsets for second corner of simplex in (i,j,k) coords
            int i2, j2, k2; // Offsets for third corner of simplex in (i,j,k) coords

            // This code would benefit from a backport from the GLSL version!
            if (x0 >= y0)
            {
                if (y0 >= z0)      { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 1; k2 = 0; } // X Y Z order
                else if (x0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 0; k2 = 1; } // X Z Y order
                else               { i1 = 0; j1 = 0; k1 = 1; i2 = 1; j2 = 0; k2 = 1; } // Z X Y order
            }
            else // x0<y0
            {
                if (y0 < z0)      { i1 = 0; j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1; } // Z Y X order
                else if (x0 < z0) { i1 = 0; j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1; } // Y Z X order
                else              { i1 = 0; j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0; } // Y X Z order
            }

            // A step of (1,0,0) in (i,j,k) means a step of (1-c,-c,-c) in (x,y,z),
            // a step of (0,1,0) in (i,j,k) means a step of (-c,1-c,-c) in (x,y,z), and
            // a step of (0,0,1) in (i,j,k) means a step of (-c,-c,1-c) in (x,y,z), where
            // c = 1/6.

            float x1 = x0 - i1 + G3; // Offsets for second corner in (x,y,z) coords
            float y1 = y0 - j1 + G3;
            float z1 = z0 - k1 + G3;
            float x2 = x0 - i2 + 2.0f * G3; // Offsets for third corner in (x,y,z) coords
            float y2 = y0 - j2 + 2.0f * G3;
            float z2 = z0 - k2 + 2.0f * G3;
            float x3 = x0 - 1.0f + 3.0f * G3; // Offsets for last corner in (x,y,z) coords
            float y3 = y0 - 1.0f + 3.0f * G3;
            float z3 = z0 - 1.0f + 3.0f * G3;

            // Wrap the integer indices at 256, to avoid indexing perm[] out of bounds
            //int ii = i % 256;
            //int jj = j % 256;
            //int kk = k % 256;

            // fix to allow input negative numbers
            int ii = _ClampCircularIndex(i, 256);
            int jj = _ClampCircularIndex(j, 256);
            int kk = _ClampCircularIndex(k, 256);

            // Calculate the contribution from the four corners
            float t0 = 0.6f - x0 * x0 - y0 * y0 - z0 * z0;
            if (t0 < 0.0f) n0 = 0.0f;
            else
            {
                t0 *= t0;
                n0 = t0 * t0 * _Grad(_PermutationTable[ii + _PermutationTable[jj + _PermutationTable[kk]]], x0, y0, z0);
            }

            float t1 = 0.6f - x1 * x1 - y1 * y1 - z1 * z1;
            if (t1 < 0.0f) n1 = 0.0f;
            else
            {
                t1 *= t1;
                n1 = t1 * t1 * _Grad(_PermutationTable[ii + i1 + _PermutationTable[jj + j1 + _PermutationTable[kk + k1]]], x1, y1, z1);
            }

            float t2 = 0.6f - x2 * x2 - y2 * y2 - z2 * z2;
            if (t2 < 0.0f) n2 = 0.0f;
            else
            {
                t2 *= t2;
                n2 = t2 * t2 * _Grad(_PermutationTable[ii + i2 + _PermutationTable[jj + j2 + _PermutationTable[kk + k2]]], x2, y2, z2);
            }

            float t3 = 0.6f - x3 * x3 - y3 * y3 - z3 * z3;
            if (t3 < 0.0f) n3 = 0.0f;
            else
            {
                t3 *= t3;
                n3 = t3 * t3 * _Grad(_PermutationTable[ii + 1 + _PermutationTable[jj + 1 + _PermutationTable[kk + 1]]], x3, y3, z3);
            }

            // Add contributions from each corner to get the final noise value.
            // The result is scaled to stay just inside [-1,1]
            return SCALE3D * (n0 + n1 + n2 + n3);

        }

        public static float Noise(float x, float y, float z, float w)
        {
            //System.Diagnostics.Debug.Assert(_PermutationTable.Length == 512);

            // The skewing and unskewing factors are hairy again for the 4D case
            const float F4 = 0.309016994f; // F4 = (Math.sqrt(5.0)-1.0)/4.0
            const float G4 = 0.138196601f; // G4 = (5.0-Math.sqrt(5.0))/20.0

            float n0, n1, n2, n3, n4; // Noise contributions from the five corners

            // Skew the (x,y,z,w) space to determine which cell of 24 simplices we're in
            float s = (x + y + z + w) * F4; // Factor for 4D skewing
            float xs = x + s;
            float ys = y + s;
            float zs = z + s;
            float ws = w + s;
            int i = _FastFloor(xs);
            int j = _FastFloor(ys);
            int k = _FastFloor(zs);
            int l = _FastFloor(ws);

            float t = (i + j + k + l) * G4; // Factor for 4D unskewing
            float X0 = i - t; // Unskew the cell origin back to (x,y,z,w) space
            float Y0 = j - t;
            float Z0 = k - t;
            float W0 = l - t;

            float x0 = x - X0;  // The x,y,z,w distances from the cell origin
            float y0 = y - Y0;
            float z0 = z - Z0;
            float w0 = w - W0;

            // For the 4D case, the simplex is a 4D shape I won't even try to describe.
            // To find out which of the 24 possible simplices we're in, we need to
            // determine the magnitude ordering of x0, y0, z0 and w0.
            // The method below is a good way of finding the ordering of x,y,z,w and
            // then find the correct traversal order for the simplex we’re in.
            // First, six pair-wise comparisons are performed between each possible pair
            // of the four coordinates, and the results are used to add up binary bits
            // for an integer index.
            int c1 = (x0 > y0) ? 32 : 0;
            int c2 = (x0 > z0) ? 16 : 0;
            int c3 = (y0 > z0) ? 8 : 0;
            int c4 = (x0 > w0) ? 4 : 0;
            int c5 = (y0 > w0) ? 2 : 0;
            int c6 = (z0 > w0) ? 1 : 0;
            int c = c1 + c2 + c3 + c4 + c5 + c6;

            int i1, j1, k1, l1; // The integer offsets for the second simplex corner
            int i2, j2, k2, l2; // The integer offsets for the third simplex corner
            int i3, j3, k3, l3; // The integer offsets for the fourth simplex corner

            // simplex[c] is a 4-vector with the numbers 0, 1, 2 and 3 in some order.
            // Many values of c will never occur, since e.g. x>y>z>w makes x<z, y<w and x<w
            // impossible. Only the 24 indices which have non-zero entries make any sense.
            // We use a thresholding to set the coordinates in turn from the largest magnitude.
            // The number 3 in the "simplex" array is at the position of the largest coordinate.
            i1 = _Simplex4[c, 0] >= 3 ? 1 : 0;
            j1 = _Simplex4[c, 1] >= 3 ? 1 : 0;
            k1 = _Simplex4[c, 2] >= 3 ? 1 : 0;
            l1 = _Simplex4[c, 3] >= 3 ? 1 : 0;
            // The number 2 in the "simplex" array is at the second largest coordinate.
            i2 = _Simplex4[c, 0] >= 2 ? 1 : 0;
            j2 = _Simplex4[c, 1] >= 2 ? 1 : 0;
            k2 = _Simplex4[c, 2] >= 2 ? 1 : 0;
            l2 = _Simplex4[c, 3] >= 2 ? 1 : 0;
            // The number 1 in the "simplex" array is at the second smallest coordinate.
            i3 = _Simplex4[c, 0] >= 1 ? 1 : 0;
            j3 = _Simplex4[c, 1] >= 1 ? 1 : 0;
            k3 = _Simplex4[c, 2] >= 1 ? 1 : 0;
            l3 = _Simplex4[c, 3] >= 1 ? 1 : 0;
            // The fifth corner has all coordinate offsets = 1, so no need to look that up.

            float x1 = x0 - i1 + G4; // Offsets for second corner in (x,y,z,w) coords
            float y1 = y0 - j1 + G4;
            float z1 = z0 - k1 + G4;
            float w1 = w0 - l1 + G4;
            float x2 = x0 - i2 + 2.0f * G4; // Offsets for third corner in (x,y,z,w) coords
            float y2 = y0 - j2 + 2.0f * G4;
            float z2 = z0 - k2 + 2.0f * G4;
            float w2 = w0 - l2 + 2.0f * G4;
            float x3 = x0 - i3 + 3.0f * G4; // Offsets for fourth corner in (x,y,z,w) coords
            float y3 = y0 - j3 + 3.0f * G4;
            float z3 = z0 - k3 + 3.0f * G4;
            float w3 = w0 - l3 + 3.0f * G4;
            float x4 = x0 - 1.0f + 4.0f * G4; // Offsets for last corner in (x,y,z,w) coords
            float y4 = y0 - 1.0f + 4.0f * G4;
            float z4 = z0 - 1.0f + 4.0f * G4;
            float w4 = w0 - 1.0f + 4.0f * G4;

            // Wrap the integer indices at 256, to avoid indexing perm[] out of bounds (i,j,k,l MUST BE POSITIVE NUMBERS)
            //int ii = i % 256;
            //int jj = j % 256;
            //int kk = k % 256;
            //int ll = l % 256;

            // fix to allow input negative numbers
            int ii = _ClampCircularIndex(i, 256);
            int jj = _ClampCircularIndex(j, 256);
            int kk = _ClampCircularIndex(k, 256);
            int ll = _ClampCircularIndex(l, 256);

            // Calculate the contribution from the five corners
            float t0 = 0.6f - x0 * x0 - y0 * y0 - z0 * z0 - w0 * w0;
            if (t0 < 0.0f) n0 = 0.0f;
            else
            {
                t0 *= t0;
                n0 = t0 * t0 * _Grad(_PermutationTable[ii + _PermutationTable[jj + _PermutationTable[kk + _PermutationTable[ll]]]], x0, y0, z0, w0);
            }

            float t1 = 0.6f - x1 * x1 - y1 * y1 - z1 * z1 - w1 * w1;
            if (t1 < 0.0f) n1 = 0.0f;
            else
            {
                t1 *= t1;
                n1 = t1 * t1 * _Grad(_PermutationTable[ii + i1 + _PermutationTable[jj + j1 + _PermutationTable[kk + k1 + _PermutationTable[ll + l1]]]], x1, y1, z1, w1);
            }

            float t2 = 0.6f - x2 * x2 - y2 * y2 - z2 * z2 - w2 * w2;
            if (t2 < 0.0f) n2 = 0.0f;
            else
            {
                t2 *= t2;
                n2 = t2 * t2 * _Grad(_PermutationTable[ii + i2 + _PermutationTable[jj + j2 + _PermutationTable[kk + k2 + _PermutationTable[ll + l2]]]], x2, y2, z2, w2);
            }

            float t3 = 0.6f - x3 * x3 - y3 * y3 - z3 * z3 - w3 * w3;
            if (t3 < 0.0f) n3 = 0.0f;
            else
            {
                t3 *= t3;
                n3 = t3 * t3 * _Grad(_PermutationTable[ii + i3 + _PermutationTable[jj + j3 + _PermutationTable[kk + k3 + _PermutationTable[ll + l3]]]], x3, y3, z3, w3);
            }

            float t4 = 0.6f - x4 * x4 - y4 * y4 - z4 * z4 - w4 * w4;
            if (t4 < 0.0f) n4 = 0.0f;
            else
            {
                t4 *= t4;
                n4 = t4 * t4 * _Grad(_PermutationTable[ii + 1 + _PermutationTable[jj + 1 + _PermutationTable[kk + 1 + _PermutationTable[ll + 1]]]], x4, y4, z4, w4);
            }

            // Sum up and scale the result to cover the range [-1,1]
            return SCALE4D * (n0 + n1 + n2 + n3 + n4);
        }

        public static void Test()
        {
            float d1Min, d1Max, d2Min, d2Max, d3Min, d3Max;

            d1Min = d2Min = d3Min = float.MaxValue;
            d1Max = d2Max = d3Max = float.MinValue;


            for (float x = 0; x < 2506; x += 0.01f)
            {
                float d1 = Noise(x);

                d1Min = System.Math.Min(d1Min, d1);
                d1Max = System.Math.Max(d1Max, d1);
            }

            for (float x = 0; x < 256; x += 0.1f)
            {
                float d1 = Noise(x);

                d1Min = System.Math.Min(d1Min, d1);
                d1Max = System.Math.Max(d1Max, d1);

                for (float y = 0; y < 256; y += 0.1f)
                {
                    float d2 = Noise(x, y);                    

                    d2Min = System.Math.Min(d2Min, d2);
                    d2Max = System.Math.Max(d2Max, d2);

                    for (float z = 0; z < 256; z += 0.1f)
                    {
                        float d3 = Noise(x, y, z);

                        d3Min = System.Math.Min(d3Min, d3);
                        d3Max = System.Math.Max(d3Max, d3);
                    }
                }
            }

        }

        #endregion
    }
    
    public sealed class PerlinNoise3 : INoiseGenerator
    {
        // http://freespace.virgin.net/hugo.elias/models/m_perlin.htm

        #region lifecycle

        public PerlinNoise3(int size=256, int seed=177)
        {        
            _Size = size;
            _Indices = new int[_Size + _Size + 2];
            _Dim3Values = new XYZW[_Size + _Size + 2];

            var randomizer = new System.Random(seed);

            int size2 = _Size + _Size;

            int i;

            for (i = 0; i < _Size; i++)
            {
                _Indices[i] = i;
                //g1[i] = (Scalar) ((Random() % (s2)) - _TSize) / _TSize;

                //g2[i].X = (Scalar)((Random() % (s2)) - _TSize) / _TSize;
                //g2[i].Y = (Scalar)((Random() % (s2)) - _TSize) / _TSize;
                //g2[i] = g2[i].Normalized();

                var d3v = new XYZ
                    (
                    (Scalar)(randomizer.Next(size2) - _Size) / (Scalar)_Size, // values between +-1.0f
                    (Scalar)(randomizer.Next(size2) - _Size) / (Scalar)_Size, // values between +-1.0f
                    (Scalar)(randomizer.Next(size2) - _Size) / (Scalar)_Size  // values between +-1.0f
                    );

                d3v = XYZ.Normalize(d3v);

                _Dim3Values[i] = new XYZW(d3v, 0);
            }

            while (--i != 0)
            {
                int k = _Indices[i];
                int j = randomizer.Next(_Size);
                _Indices[i] = _Indices[j];
                _Indices[j] = k;
            }

            for (i = 0; i < _Size + 2; i++)
            {
                _Indices[_Size + i] = _Indices[i];
                //g1[_size + i] = g1[i];
                //g2[_TSize + i] = g2[i];
                _Dim3Values[_Size + i] = _Dim3Values[i];
            }
        }

        #endregion

        #region Data

        private readonly int _Size = 256;
        private readonly int[] _Indices;
        private readonly XYZW[] _Dim3Values;	// should be Vector4 for openCl compatibility        

        #endregion        

        #region internals

        private static Scalar s_curve(Scalar t) { return t * t * (3.0f - 2.0f * t); }
        private static Scalar _Lerp(Scalar t, Scalar a, Scalar b) { return a + t * (b - a); }
        private static Scalar _At3(Scalar rx, Scalar ry, Scalar rz, XYZW _q) { return rx * _q.X + ry * _q.Y + rz * _q.Z; }

        private struct _DotStruct
        {
            public int b0, b1;
            public Scalar r0, r1;

            public _DotStruct(Scalar v, int size)
            {
                Scalar t = v + (Scalar)(size * 16);

                b0 = (int)t & (size - 1);
                b1 = (b0 + 1) & (size - 1); // next point

                r0 = t - (int)t; // decimal part
                r1 = r0 - 1.0f;
            }
        }

        private Scalar _Noise3(XYZ vec)
        {
            _DotStruct xx = new _DotStruct(vec.X, _Size);
            _DotStruct yy = new _DotStruct(vec.Y, _Size);
            _DotStruct zz = new _DotStruct(vec.Z, _Size);

            int i = _Indices[xx.b0];
            int j = _Indices[xx.b1];

            int b00 = _Indices[i + yy.b0];
            int b10 = _Indices[j + yy.b0];
            int b01 = _Indices[i + yy.b1];
            int b11 = _Indices[j + yy.b1];

            Scalar sx = s_curve(xx.r0);
            Scalar sy = s_curve(yy.r0);
            Scalar sz = s_curve(zz.r0);

            Scalar u, v;

            u = _At3(xx.r0, yy.r0, zz.r0, _Dim3Values[b00 + zz.b0]);
            v = _At3(xx.r1, yy.r0, zz.r0, _Dim3Values[b10 + zz.b0]);
            Scalar a = _Lerp(sx, u, v);

            u = _At3(xx.r0, yy.r1, zz.r0, _Dim3Values[b01 + zz.b0]);
            v = _At3(xx.r1, yy.r1, zz.r0, _Dim3Values[b11 + zz.b0]);
            Scalar b = _Lerp(sx, u, v);

            Scalar c = _Lerp(sy, a, b);

            u = _At3(xx.r0, yy.r0, zz.r1, _Dim3Values[b00 + zz.b1]);
            v = _At3(xx.r1, yy.r0, zz.r1, _Dim3Values[b10 + zz.b1]);
            a = _Lerp(sx, u, v);

            u = _At3(xx.r0, yy.r1, zz.r1, _Dim3Values[b01 + zz.b1]);
            v = _At3(xx.r1, yy.r1, zz.r1, _Dim3Values[b11 + zz.b1]);
            b = _Lerp(sx, u, v);

            Scalar d = _Lerp(sy, a, b);

            return _Lerp(sz, c, d);
        }        

        #endregion

        #region API

        public int[] IndexTable { get { return _Indices; } }

        public XYZW[] VectorTable { get { return _Dim3Values; } }

        public Scalar Noise(XYZ loc, Scalar alpha = 1, Scalar beta = 1, int n = 1)
        {
            Scalar sum = 0;
            Scalar scale = 1.0f;
            XYZ p = loc;

            for (int i = 0; i < n; i++)
            {
                Scalar val = _Noise3(p);
                sum += val / scale;
                scale *= alpha;
                p.X *= beta;
                p.Y *= beta;
                p.Z *= beta;
            }           

            return sum;
        }

        public Scalar Noise(float x, float y, float z)
        {
            return Noise(new XYZ(x, y, z));
        }

        public Scalar Noise(float x, float y, float z , int degree = 3)
        {
            return Noise(new XYZ(x, y, z), degree,1.1f,degree);
        }

        #endregion

        #region INoiseGenerator Members

        public Scalar GetSample(Scalar x)
        {
            Scalar v = Noise(x, 0, 0); System.Diagnostics.Debug.Assert(v >= -1 && v <= 1);
            return v.Clamped(-1, 1);
        }

        public Scalar GetSample(Scalar x, Scalar y)
        {
            Scalar v = Noise(x, y, 0); System.Diagnostics.Debug.Assert(v >= -1 && v <= 1);
            return v.Clamped(-1, 1);
        }

        public Scalar GetSample(Scalar x, Scalar y,Scalar z)
        {
            Scalar v = Noise(x, y, z); System.Diagnostics.Debug.Assert(v >= -1 && v <= 1);
            return v.Clamped(-1,1);
        }

        #endregion
    }
    
    public sealed class ImprovedPerlinNoise : INoiseGenerator
    {
        // http://mrl.nyu.edu/~perlin/noise/

        // JAVA REFERENCE IMPLEMENTATION OF IMPROVED NOISE - COPYRIGHT 2002 KEN PERLIN.

        public ImprovedPerlinNoise(int seed = 177)
        {
            var rnd = new System.Random(seed);

            for (int i = 0; i < 256; ++i)
            {
                _Permutation[i] = _Permutation[i + 256] = rnd.Next(256);
            }
        }

        private readonly int[] _Permutation = new int[512];

        public double Noise(double x, double y, double z)
        {
            int xx = (int)System.Math.Floor(x) & 255,                  // FIND UNIT CUBE THAT
                yy = (int)System.Math.Floor(y) & 255,                  // CONTAINS POINT.
                zz = (int)System.Math.Floor(z) & 255;

            x -= System.Math.Floor(x);                                // FIND RELATIVE X,Y,Z
            y -= System.Math.Floor(y);                                // OF POINT IN CUBE.
            z -= System.Math.Floor(z);

            double u = _Fade(x),                                // COMPUTE FADE CURVES
                   v = _Fade(y),                                // FOR EACH OF X,Y,Z.
                   w = _Fade(z);

            int a = _Permutation[xx] + yy,     aa = _Permutation[a] + zz, ab = _Permutation[a + 1] + zz,      // HASH COORDINATES OF
                b = _Permutation[xx + 1] + yy, ba = _Permutation[b] + zz, bb = _Permutation[b + 1] + zz;      // THE 8 CUBE CORNERS,

            // AND ADD BLENDED RESULTS FROM 8 CORNERS OF CUBE
            return _Lerp(w, _Lerp(v, _Lerp(u, _Grad(_Permutation[aa], x, y, z),            _Grad(_Permutation[ba], x - 1, y, z)),
                                     _Lerp(u, _Grad(_Permutation[ab], x, y - 1, z),        _Grad(_Permutation[bb], x - 1, y - 1, z))),
                            _Lerp(v, _Lerp(u, _Grad(_Permutation[aa + 1], x, y, z - 1),    _Grad(_Permutation[ba + 1], x - 1, y, z - 1)),
                                     _Lerp(u, _Grad(_Permutation[ab + 1], x, y - 1, z - 1),_Grad(_Permutation[bb + 1], x - 1, y - 1, z - 1)))
                        );
        }

        private static double _Fade(double t) { return t * t * t * (t * (t * 6 - 15) + 10); }

        private static double _Lerp(double t, double a, double b) { return a + t * (b - a); }

        private static double _Grad(int hash, double x, double y, double z)
        {
            int h = hash & 15;                      // CONVERT LO 4 BITS OF HASH CODE
            double u = h < 8 ? x : y,                 // INTO 12 GRADIENT DIRECTIONS.
                   v = h < 4 ? y : h == 12 || h == 14 ? x : z;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }
        
        #region INoiseGenerator Members

        public Scalar GetSample(Scalar x)
        {
            Scalar v = (Scalar)Noise(x, 0, 0); System.Diagnostics.Debug.Assert(v >= -1 && v <= 1);
            return v.Clamped(-1, 1);
        }

        public Scalar GetSample(Scalar x, Scalar y)
        {
            Scalar v = (Scalar)Noise(x, y, 0); System.Diagnostics.Debug.Assert(v >= -1 && v <= 1);
            return v.Clamped(-1, 1);
        }

        public Scalar GetSample(Scalar x, Scalar y, Scalar z)
        {
            Scalar v = (Scalar)Noise(x, y, z); System.Diagnostics.Debug.Assert(v >= -1 && v <= 1);
            return v.Clamped(-1, 1);
        }

        #endregion
    }




    public class Perlin_Tiled : INoiseGenerator
    {
        // http://flafla2.github.io/2014/08/09/perlinnoise.html

        public Perlin_Tiled(int repeat = -1)
        {
            this.repeat = repeat;
        }

        static Perlin_Tiled()
        {
            p = new int[512];
            for (int x = 0; x < 512; x++)
            {
                p[x] = permutation[x % 256];
            }
        }

        public int repeat;

        public double OctavePerlin(double x, double y, double z, int octaves, double persistence)
        {
            double total = 0;
            double frequency = 1;
            double amplitude = 1;
            double maxValue = 0;            // Used for normalizing result to 0.0 - 1.0
            for (int i = 0; i < octaves; i++)
            {
                total += perlin(x * frequency, y * frequency, z * frequency) * amplitude;

                maxValue += amplitude;

                amplitude *= persistence;
                frequency *= 2;
            }

            return total / maxValue;
        }

        private static readonly int[] permutation = { 151,160,137,91,90,15,					// Hash lookup table as defined by Ken Perlin.  This is a randomly
		131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,	// arranged array of all numbers from 0-255 inclusive.
		190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
        88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
        77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
        102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
        135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
        5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
        223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
        129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
        251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
        49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
        138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
    };

        private static readonly int[] p;                                                    // Doubled permutation to avoid overflow



        public double perlin(double x, double y, double z)
        {
            if (repeat > 0)
            {                                   // If we have any repeat on, change the coordinates to their "local" repetitions
                x = x % repeat;
                y = y % repeat;
                z = z % repeat;
            }

            int xi = (int)x & 255;                              // Calculate the "unit cube" that the point asked will be located in
            int yi = (int)y & 255;                              // The left bound is ( |_x_|,|_y_|,|_z_| ) and the right bound is that
            int zi = (int)z & 255;                              // plus 1.  Next we calculate the location (from 0.0 to 1.0) in that cube.
            double xf = x - (int)x;                             // We also fade the location to smooth the result.
            double yf = y - (int)y;


            double zf = z - (int)z;
            double u = fade(xf);
            double v = fade(yf);
            double w = fade(zf);

            int aaa, aba, aab, abb, baa, bba, bab, bbb;
            aaa = p[p[p[xi] + yi] + zi];
            aba = p[p[p[xi] + inc(yi)] + zi];
            aab = p[p[p[xi] + yi] + inc(zi)];
            abb = p[p[p[xi] + inc(yi)] + inc(zi)];
            baa = p[p[p[inc(xi)] + yi] + zi];
            bba = p[p[p[inc(xi)] + inc(yi)] + zi];
            bab = p[p[p[inc(xi)] + yi] + inc(zi)];
            bbb = p[p[p[inc(xi)] + inc(yi)] + inc(zi)];

            double x1, x2, y1, y2;
            x1 = lerp(grad(aaa, xf, yf, zf),                // The gradient function calculates the dot product between a pseudorandom
                        grad(baa, xf - 1, yf, zf),              // gradient vector and the vector from the input coordinate to the 8
                        u);                                     // surrounding points in its unit cube.
            x2 = lerp(grad(aba, xf, yf - 1, zf),                // This is all then lerped together as a sort of weighted average based on the faded (u,v,w)
                        grad(bba, xf - 1, yf - 1, zf),              // values we made earlier.
                          u);
            y1 = lerp(x1, x2, v);

            x1 = lerp(grad(aab, xf, yf, zf - 1),
                        grad(bab, xf - 1, yf, zf - 1),
                        u);
            x2 = lerp(grad(abb, xf, yf - 1, zf - 1),
                          grad(bbb, xf - 1, yf - 1, zf - 1),
                          u);
            y2 = lerp(x1, x2, v);

            return (lerp(y1, y2, w) + 1) / 2;                       // For convenience we bound it to 0 - 1 (theoretical min/max before is -1 - 1)
        }

        private int inc(int num)
        {
            num++;
            if (repeat > 0) num %= repeat;

            return num;
        }

        private static double grad(int hash, double x, double y, double z)
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

        private static double fade(double t)
        {
            // Fade function as defined by Ken Perlin.  This eases coordinate values
            // so that they will "ease" towards integral values.  This ends up smoothing
            // the final output.
            return t * t * t * (t * (t * 6 - 15) + 10);         // 6t^5 - 15t^4 + 10t^3
        }

        private static double lerp(double a, double b, double x)
        {
            return a + x * (b - a);
        }



        #region INoiseGenerator Members

        public Scalar GetSample(Scalar x)
        {
            Scalar v = (Scalar)perlin(x, 0, 0); System.Diagnostics.Debug.Assert(v >= -1 && v <= 1);
            return v.Clamped(-1, 1);
        }

        public Scalar GetSample(Scalar x, Scalar y)
        {
            Scalar v = (Scalar)perlin(x, y, 0); System.Diagnostics.Debug.Assert(v >= -1 && v <= 1);
            return v.Clamped(-1, 1);
        }

        public Scalar GetSample(Scalar x, Scalar y, Scalar z)
        {
            Scalar v = (Scalar)perlin(x, y, z); System.Diagnostics.Debug.Assert(v >= -1 && v <= 1);
            return v.Clamped(-1, 1);
        }

        #endregion
    }
}
