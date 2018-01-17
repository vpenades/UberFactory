using System;
using System.Collections.Generic;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Brushes;

namespace Epsylon.ImageSharp.Procedural
{   

    using V2 = System.Numerics.Vector2;
    using P2 = SixLabors.Primitives.Point;


    // todo: it probably needs to be disposable, because if requiting
    // to create extra resources, it needs to be disposed
    public interface ISampler<TCoord, TPixel> where TPixel : struct, IPixel<TPixel>
    {
        /// <summary>
        /// gets the average pixel value contained within the polygon defined by a,b,c and d
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        TPixel GetSample(TCoord a, TCoord b, TCoord c, TCoord d);
    }

    public static class SamplerFactory
    {
        public static ISampler<V2,TPixel> CreateSampler<TPixel>(this Image<TPixel> image) where TPixel : struct, IPixel<TPixel>
        {
            return new _NearestNeighbourSampler<TPixel>(image);
        }
    }
        


    class _NearestNeighbourSampler<TPixel> : ISampler<V2,TPixel> where TPixel : struct, IPixel<TPixel>
    {
        public _NearestNeighbourSampler(Image<TPixel> image)
        {
            _Source = image;
            _Size = new V2(image.Width, image.Height);
        }

        private readonly Image<TPixel> _Source;
        private readonly V2 _Size;        

        public TPixel GetSample(V2 a, V2 b, V2 c, V2 d)
        {
            var p = (a + b + c + d) * _Size * 0.25f;

            int x = _Source.GetWrappedX((int)p.X);
            int y = _Source.GetWrappedY((int)p.Y);

            return _Source[x, y];
        }
    }
    
    class _NormalizeUVTransformSampler<TPixel> : ISampler<P2, TPixel> where TPixel : struct, IPixel<TPixel>
    {
        public _NormalizeUVTransformSampler(ISampler<V2, TPixel> source, Image<TPixel> image)
        {
            _Source = source;
            _InvSize = V2.One / new V2(image.Width, image.Height);
        }

        private readonly ISampler<V2, TPixel> _Source;
        private readonly V2 _InvSize;        

        private static readonly V2 _HALF = V2.One / 2;

        public TPixel GetSample(P2 a, P2 b, P2 c, P2 d)
        {
            var aa = new V2(a.X, a.Y);
            var bb = new V2(b.X, b.Y);
            var cc = new V2(c.X, c.Y);
            var dd = new V2(d.X, d.Y);

            aa = (aa + _HALF) * _InvSize;
            bb = (bb + _HALF) * _InvSize;
            cc = (cc + _HALF) * _InvSize;
            dd = (dd + _HALF) * _InvSize;

            return _Source.GetSample(aa, bb, cc, dd);
        }        
    }

    /// <summary>
    /// samples the source aplying a polar coordinate transform
    /// </summary>
    /// <typeparam name="TPixel"></typeparam>
    class _PolarTransformSampler<TPixel> : ISampler<V2, TPixel> where TPixel : struct, IPixel<TPixel>
    {
        public _PolarTransformSampler(ISampler<V2, TPixel> source)
        {
            _Source = source;
        }

        private readonly ISampler<V2, TPixel> _Source;

        private static readonly V2 _HALF = V2.One / 2;                

        public TPixel GetSample(V2 a, V2 b, V2 c, V2 d)
        {
            a = _PolarTransform(a);
            b = _PolarTransform(b);
            c = _PolarTransform(c);
            d = _PolarTransform(d);

            return _Source.GetSample(a, b, c, d);
        }

        private V2 _PolarTransform(V2 p)
        {
            p -= _HALF; // offset coords to the center of the image

            var angle = -Math.Atan2(p.X, p.Y);
            angle += Math.PI;
            angle /= Math.PI * 2;

            p /= _HALF; // normalize

            var radius = p.Length();

            return new V2((float)angle, 1 - (float)radius);
        }
    }
}
