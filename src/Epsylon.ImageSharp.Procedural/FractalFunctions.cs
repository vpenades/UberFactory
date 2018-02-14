using System;
using System.Collections.Generic;
using System.Text;
using SixLabors.Primitives;

namespace Epsylon.ImageSharp.Procedural
{
    // https://github.com/mgechev/mandelbrot-set/tree/master/MandelbrotSet/Model

    public class MandelbrotFractal : ITextureSampler<float>
    {
        public MandelbrotFractal(int width, int height)
        {
            _Size = new SizeF(width,height);            
        }
        
        private readonly SizeF _Size;

        private Double _OffsetX = 0.5;
        private Double _OffsetY = 0.5;
        private Double _Scale = 1;
        private int _Iterations = 100;

        public SizeF Scale => _Size;

        public Double OffsetX { get => _OffsetX; set => _OffsetX = value; }
        public Double OffsetY { get => _OffsetY; set => _OffsetY = value; }
        public Double FractalScale { get => _Scale; set => _Scale = value; }
        public int Iterations { get => _Iterations; set => _Iterations = value; }

        public float GetAreaSample(PointF tl, PointF tr, PointF br, PointF bl)
        {
            return GetPointSample((tl + tr + br + bl) * 0.25f);
        }

        public float GetPointSample(PointF uv)
        {
            double x = uv.X;
            double y = uv.Y;

            double x0 = (_OffsetX - x / _Size.Width) * _Scale;
            double y0 = (_OffsetY - y / _Size.Height) * _Scale;

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
