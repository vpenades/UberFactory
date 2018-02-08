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
    using System.Numerics;
    using System.Runtime.CompilerServices;


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
        public static ISampler<V2,TPixel> CreateSampler<TPixel>(this Image<TPixel> image, SamplerAddressMode u = SamplerAddressMode.Wrap, SamplerAddressMode v = SamplerAddressMode.Wrap) where TPixel : struct, IPixel<TPixel>
        {
            return new _NearestNeighbourSampler<TPixel>(image,u,v);
        }

        public static ISampler<V2,TPixel> ToPolarTransform<TPixel>(this ISampler<V2, TPixel> sampler) where TPixel : struct, IPixel<TPixel>
        {
            return new _PolarTransformSampler<TPixel>(sampler);
        }

        public static ISampler<P2,TPixel> ToPointSampler<TPixel>(this ISampler<V2,TPixel> sampler, int w, int h) where TPixel : struct, IPixel<TPixel>
        {
            return new _NormalizeUVTransformSampler<TPixel>(sampler, w, h);
        }
    }

    class _NormalizeUVTransformSampler<TPixel> : ISampler<P2, TPixel> where TPixel : struct, IPixel<TPixel>
    {
        public _NormalizeUVTransformSampler(ISampler<V2, TPixel> source, int w, int h)
        {
            _Source = source;
            _InvSize = V2.One / new V2(w, h);
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

    public enum SamplerAddressMode
    {
        Wrap = 1,
        Mirror = 2,
        Clamp = 3,
        // Border = 4, // this has been deprecated in most hardware implementations since it requires an extra register
        MirrorOnce = 5
    }

    abstract class _ImageSampler<TPixel> where TPixel : struct, IPixel<TPixel>
    {
        public _ImageSampler(Image<TPixel> image, SamplerAddressMode u, SamplerAddressMode v)
        {
            _Source = image;
            _Size = new V2(image.Width, image.Height);
            _AddressU = u;
            _AddressV = v;
        }

        private readonly Image<TPixel> _Source;
        private readonly V2 _Size;
        private readonly SamplerAddressMode _AddressU;
        private readonly SamplerAddressMode _AddressV;

        private static readonly V2 _HALF = V2.One / 2;

        

        protected TPixel this[int x, int y]
        {
            get
            {
                x = _AddressU.GetFinalIndex(x, _Source.Width);
                y = _AddressV.GetFinalIndex(y, _Source.Height);

                return _Source[x, y];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected TPixel GetNearestPixel(V2 p)
        {
            p *= _Size;

            return this[(int)p.X,(int)p.Y];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected TPixel GetBilinearPixel(V2 p)
        {
            p *= _Size;
            p -= _HALF;

            int ix = p.X.RoundedTextureCoordinate();
            int iy = p.Y.RoundedTextureCoordinate();

            var A = this[ix,     iy    ].ToVector4();
            var B = this[ix + 1, iy    ].ToVector4();
            var C = this[ix,     iy + 1].ToVector4();
            var D = this[ix + 1, iy + 1].ToVector4();

            p.X -= ix;
            p.Y -= iy;

            A = A.AlphaAwareLerp(B, p.X); // first row
            C = C.AlphaAwareLerp(D, p.X); // second row
            A = A.AlphaAwareLerp(C, p.Y); // column

            var v = default(TPixel);
            v.PackFromVector4(A);

            return v;
        }
    }

    class _NearestNeighbourSampler<TPixel> : _ImageSampler<TPixel> , ISampler<V2, TPixel> where TPixel : struct, IPixel<TPixel>
    {
        public _NearestNeighbourSampler(Image<TPixel> image, SamplerAddressMode u, SamplerAddressMode v) : base(image,u,v) { }        

        public TPixel GetSample(V2 a, V2 b, V2 c, V2 d)
        {
            var p = (a + b + c + d) * 0.25f;
            return this.GetNearestPixel(p);
        }
    }

    class _BilinearSampler<TPixel> : _ImageSampler<TPixel>, ISampler<V2, TPixel> where TPixel : struct, IPixel<TPixel>
    {
        public _BilinearSampler(Image<TPixel> image, SamplerAddressMode u, SamplerAddressMode v) : base(image, u, v) { }

        public TPixel GetSample(V2 a, V2 b, V2 c, V2 d)
        {
            var p = (a + b + c + d) * 0.25f;
            return this.GetNearestPixel(p);
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
