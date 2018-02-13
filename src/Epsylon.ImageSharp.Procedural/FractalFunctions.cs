using System;
using System.Collections.Generic;
using System.Text;
using SixLabors.Primitives;

namespace Epsylon.ImageSharp.Procedural
{
    // https://github.com/mgechev/mandelbrot-set/tree/master/MandelbrotSet/Model

    public class MandelbrotFractal : ITextureSampler<float>
    {
        public MandelbrotFractal(int width, int height, float scale, int iterations)
        {
            _Size = new SizeF(width,height);
            _Scale = scale;
            _Iterations = iterations;            
        }

        private int _Iterations;
        private SizeF _Size;
        private double _Scale;

        public SizeF Scale => _Size;

        public float GetAreaSample(PointF tl, PointF tr, PointF br, PointF bl)
        {
            return GetPointSample((tl + tr + br + bl) * 0.25f);
        }

        public float GetPointSample(PointF uv)
        {
            double x = uv.X;
            double y = uv.Y;

            double x0 = (-x / _Size.Width) * _Scale;
            double y0 = (-y / _Size.Height) * _Scale;

            x = 0;
            y = 0;

            var i = 0;
            double tempX = 0;

            while (x * x + y * y < 4 && i < _Iterations)
            {
                tempX = x * x - y * y + x0;
                y = 2 * x * y + y0;

                x = tempX;
                i += 1;
            }

            return (float)i/(float)_Iterations;
        }

        
    }
}
