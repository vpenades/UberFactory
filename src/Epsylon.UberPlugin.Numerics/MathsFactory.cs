using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberPlugin.Numerics
{
    using VECTOR = MathNet.Numerics.LinearAlgebra.Double.Vector;

    static class VectorsFactory
    {
        public static VECTOR Create(params double[] args)
        {
            return VECTOR.Build.DenseOfArray(args) as VECTOR;
        }

        public static VECTOR Add(VECTOR a, VECTOR b)
        {
            return (VECTOR)a.Add(b);
        }
    }
}
