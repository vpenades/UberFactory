using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.MetaData;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;
using System;
using System.Numerics;

namespace Epsylon.ImageSharp.Procedural
{
    using UV = PointF;    

    public interface ITextureSampler<TPixel>
    {        
        SizeF Scale { get; }

        TPixel GetPointSample(UV uv);

        TPixel GetAreaSample(UV tl, UV tr, UV br, UV bl);
    }

    public static class TextureSamplerFactory
    {
        public static ITextureSampler<Vector4> ToTextureSampler(this IPixelSampler ps, bool normalizedUV)
        {
            return new _PixelTextureSampler(ps, normalizedUV);
        }

        public static ITextureSampler<TPixel> ToPolarTransform<TPixel>(this ITextureSampler<TPixel> ts, bool inverse)
        {
            return new _PolarTransformedTextureSampler<TPixel>(ts, inverse);
        }

        public static ITextureSampler<float> CreatePerlinNoiseTexture(int repeat, int octaves, float persistence, int randomSeed = 177)
        {
            return _PerlinNoiseTextureSampler.Create(repeat, octaves, persistence, randomSeed);
        }

        public static ITextureSampler<float> CreateMandelbrotTexture(int width, int height, double offsetX, double offsetY, double scale, int iterations)
        {
            var fractal = new MandelbrotFractal(width, height)
            {
                OffsetX = offsetX,
                OffsetY = offsetY,
                FractalScale = scale,
                Iterations = iterations
            };

            return fractal;
        }

        public static ITextureSampler<HalfSingle> ToHalfSingle(this ITextureSampler<float> source)
        {
            return new _FloatToHalfSingleSampler(source);
        }

        public static ITextureSampler<Vector4> LerpColor(this ITextureSampler<float> source, IPixel odd, IPixel even)
        {
            return new _LerpColorSampler(source, odd, even);
        }

        public static void Fill<TPixel>(this Image<TPixel> target, ITextureSampler<Vector4> sampler) where TPixel : struct, IPixel<TPixel>
        {
            var scale = new Vector2(sampler.Scale.Width / (float)target.Width, sampler.Scale.Height  / (float)target.Height);

            for (int dy = 0; dy < target.Height; ++dy)
            {
                var tl = default(UV);
                var tr = default(UV);
                var bl = default(UV);
                var br = default(UV);

                tl.Y = tr.Y = dy;
                bl.Y = br.Y = dy + 1;

                var c = default(TPixel);

                for (int dx = 0; dx < target.Width; ++dx)
                {
                    tl.X = bl.X = dx;
                    tr.X = br.X = dx + 1;

                    var r = sampler.GetAreaSample(tl * scale, tr * scale, br * scale, bl * scale);

                    c.PackFromVector4(r);

                    target[dx, dy] = c;
                }
            }
        }

        public static void Fill<TPixel>(this Image<TPixel> target, ITextureSampler<TPixel> sampler) where TPixel : struct, IPixel<TPixel>
        {
            var scale = new Vector2(sampler.Scale.Width / (float)target.Width, sampler.Scale.Height / (float)target.Height);

            for (int dy = 0; dy < target.Height; ++dy)
            {
                var tl = default(UV);
                var tr = default(UV);
                var bl = default(UV);
                var br = default(UV);

                tl.Y = tr.Y = dy;
                bl.Y = br.Y = dy + 1;

                var c = default(TPixel);

                for (int dx = 0; dx < target.Width; ++dx)
                {
                    tl.X = bl.X = dx;
                    tr.X = br.X = dx + 1;

                    var r = sampler.GetAreaSample(tl * scale, tr * scale, br * scale, bl * scale);                    

                    target[dx, dy] = r;
                }
            }
        }
    }


    class _PixelTextureSampler : ITextureSampler<Vector4>
    {
        #region lifecycle

        public _PixelTextureSampler(IPixelSampler source, bool normalizedUV)
        {
            _Source = source;            

            if (normalizedUV)
            {
                _Size = new SizeF(1, 1);
                _Scale = new Vector2(_Source.Width, source.Height);
            }
            else
            {
                _Size = new SizeF(_Source.Width, _Source.Height);
                _Scale = Vector2.One;
            }

            _Offset = -Vector2.One / 2;
        }        

        #endregion

        #region data        

        private readonly IPixelSampler _Source;
        private readonly SizeF _Size;

        private readonly Vector2 _Scale;
        private readonly Vector2 _Offset;

        #endregion

        #region properties

        public SizeF Scale => _Size;        

        #endregion

        #region API

        public Vector4 GetPointSample(UV uv_)
        {
            Vector2 uv = uv_;

            uv *= _Scale;
            uv += _Offset;

            int x = (int)(uv.X >= 0 ? uv.X : uv.X - 1);
            int y = (int)(uv.Y >= 0 ? uv.Y : uv.Y - 1);            

            var A = _Source[x+0, y+0];
            var B = _Source[x+1, y+0];
            var C = _Source[x+0, y+1];
            var D = _Source[x+1, y+1];

            uv.X -= x; System.Diagnostics.Debug.Assert(uv.X >= 0 && uv.X <= 1);
            uv.Y -= y; System.Diagnostics.Debug.Assert(uv.Y >= 0 && uv.Y <= 1);

            // bilinear filtering
            A = A.AlphaAwareLerp(B, uv.X); // first row
            C = C.AlphaAwareLerp(D, uv.X); // second row

            return A.AlphaAwareLerp(C, uv.Y); // column            
        }

        public Vector4 GetAreaSample(UV tl, UV tr, UV br, UV bl)
        {
            // TODO: a more precise implementation would traverse a tree of
            // mipmapped /anisotropic images until the area is ~1 and do point sample.

            var p = ((Vector2)tl + (Vector2)tr + (Vector2)br + (Vector2)bl) / 4; // provisional workaround

            return GetPointSample(p);
        }

        #endregion
    }    

    abstract class _TransformedTextureSampler<TPixel> : ITextureSampler<TPixel>
    {
        #region lifecycle

        public _TransformedTextureSampler(ITextureSampler<TPixel> source)
        {
            _Source = source;
        }

        #endregion

        #region data

        private readonly ITextureSampler<TPixel> _Source;        

        #endregion

        #region properties

        public SizeF Scale => _Source.Scale;

        #endregion

        #region API        

        public TPixel GetAreaSample(UV a, UV b, UV c, UV d)
        {
            a = Transform(a);
            b = Transform(b);
            c = Transform(c);
            d = Transform(d);

            return _Source.GetAreaSample(a, b, c, d);
        }

        public TPixel GetPointSample(UV uv)
        {
            uv = Transform(uv);

            return _Source.GetPointSample(uv);
        }

        protected abstract UV Transform(UV uv);        

        #endregion
    }

    sealed class _PolarTransformedTextureSampler<TPixel> : _TransformedTextureSampler<TPixel>
    {
        #region lifecycle        

        public _PolarTransformedTextureSampler(ITextureSampler<TPixel> source, bool inverse = false) : base(source)
        {
            _Inverse = inverse;
            _Scale = source.Scale;
            _Center = _Scale / 2;

            _Center = _Center.Round(); // todo: we could "snap" the _Center of the image to an interpixel cross.
        }

        #endregion       

        #region API

        private readonly bool _Inverse;
        private readonly Vector2 _Scale;
        private readonly Vector2 _Center;

        protected override UV Transform(UV pp)
        {
            return _Inverse ? _PolarToSquare(pp) : _SquareToPolar(pp);
        }

        private UV _SquareToPolar(UV pp)
        {
            Vector2 p = pp;

            p -= _Center; // offset coords to the center of the image

            var angle = - Math.Atan2(p.X, p.Y);
            angle += Math.PI;
            angle /= Math.PI * 2;

            p /= _Center; // normalize
            var radius = p.Length();

            p = Vector2.One - new Vector2((float)angle, (float)radius);            

            return p * _Scale;
        }

        private UV _PolarToSquare(UV pp)
        {
            Vector2 p = pp;

            p /= _Scale;            

            var angle = (p.X * Math.PI * 2 - Math.PI);
            var radius = p.Y;

            var x = (float)-Math.Sin(angle);
            var y = (float)+Math.Cos(angle);            

            p = new Vector2(x, y) * radius * _Center;

            p = _Center - p;

            return p;
        }

        #endregion
    }

    sealed class _PerlinNoiseTextureSampler : ITextureSampler<float>
    {
        // TODO: pass a Black/White colors and interpolate based on noise value.

        public static _PerlinNoiseTextureSampler Create(int repeat, int octaves, float persistence, int randomSeed = 177)
        {
            return new _PerlinNoiseTextureSampler(repeat, octaves, persistence, randomSeed);
        }

        private _PerlinNoiseTextureSampler(int repeat, int octaves, float persistence, int randomSeed = 177)
        {
            if (repeat <= 0) repeat = -1; // repetition is disabled

            _Scale = repeat > 0 ? new SizeF(repeat, repeat) : new SizeF(1,1);            

            _Perlin = new Perlin_Tileable(randomSeed, repeat);

            _Depth = 0;
            _Octaves = octaves;
            _Persistence = persistence;
        }

        private readonly Perlin_Tileable _Perlin;

        private readonly SizeF _Scale;

        private readonly float _Depth;
        private readonly int _Octaves;
        private readonly float _Persistence;
        private readonly Vector4 _OddColor;
        private readonly Vector4 _EvenColor;

        public SizeF Scale => _Scale;

        public float GetAreaSample(UV tl, UV tr, UV br, UV bl)
        {
            return GetPointSample((tl + tr + br + bl) * 0.25f);
        }

        public float GetPointSample(UV uv)
        {
            var v = (float)_Perlin
                .OctavePerlin(uv.X, uv.Y, _Depth, _Octaves, _Persistence)
                .Clamp(0,1);

            return v;
        }
    }


    sealed class _LerpColorSampler : ITextureSampler<Vector4>
    {
        public _LerpColorSampler(ITextureSampler<float> source, IPixel odd, IPixel even)
        {
            _Source = source;
            _OddColor = odd.ToVector4();
            _EvenColor = even.ToVector4();
        }

        private readonly ITextureSampler<float> _Source;
        private readonly Vector4 _OddColor;
        private readonly Vector4 _EvenColor;

        public SizeF Scale => _Source.Scale;

        public Vector4 GetPointSample(UV uv)
        {
            var f = _Source.GetPointSample(uv).Clamp(0,1);
            return Vector4.Lerp(_OddColor, _EvenColor, f);
        }

        public Vector4 GetAreaSample(UV tl, UV tr, UV br, UV bl)
        {
            var f = _Source.GetAreaSample(tl,tr,br,bl).Clamp(0,1);
            return Vector4.Lerp(_OddColor, _EvenColor, f);
        }
    }

    

        sealed class _FloatToHalfSingleSampler : ITextureSampler<HalfSingle>
    {
        public _FloatToHalfSingleSampler(ITextureSampler<float> source)
        {
            _Source = source;            
        }

        private readonly ITextureSampler<float> _Source;

        public SizeF Scale => _Source.Scale;

        public HalfSingle GetPointSample(UV uv)
        {
            var f = _Source.GetPointSample(uv);
            return new HalfSingle(f);
        }

        public HalfSingle GetAreaSample(UV tl, UV tr, UV br, UV bl)
        {
            var f = _Source.GetAreaSample(tl, tr, br, bl);
            return new HalfSingle(f);
        }
    }



    // TODO: Math function evaluator:
    // http://opensource.graphics/image-processing-made-easier-with-a-powerful-math-expression-evaluator/
    // and
    // https://github.com/sheetsync/NCalc
    // or
    // https://github.com/sebastienros/jint


}
