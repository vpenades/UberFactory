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

    public interface ITextureSampler
    {        
        SizeF Scale { get; }

        Vector4 GetPointSample(UV uv);
        
        Vector4 GetAreaSample(UV tl, UV tr, UV br, UV bl);
    }

    public static class TextureSamplerFactory
    {
        public static ITextureSampler ToTextureSampler(this IPixelSampler ps, bool normalizedUV)
        {
            return new _PixelTextureSampler(ps, normalizedUV);
        }

        public static ITextureSampler ToPolarTransform(this ITextureSampler ts)
        {
            return new _PolarTransformedTextureSampler(ts);
        }

        public static ITextureSampler CreatePerlinNoiseTexture(IPixel odd, IPixel even, int repeat, int octaves, float persistence, int randomSeed = 177)
        {
            return _PerlinNoiseTextureSampler.Create(odd, even, repeat, octaves, persistence, randomSeed);
        }
    }


    class _PixelTextureSampler : ITextureSampler
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

    abstract class _TransformedTextureSampler : ITextureSampler
    {
        #region lifecycle

        public _TransformedTextureSampler(ITextureSampler source)
        {
            _Source = source;
        }

        #endregion

        #region data

        private readonly ITextureSampler _Source;        

        #endregion

        #region properties

        public SizeF Scale => _Source.Scale;

        #endregion

        #region API        

        public Vector4 GetAreaSample(UV a, UV b, UV c, UV d)
        {
            a = Transform(a);
            b = Transform(b);
            c = Transform(c);
            d = Transform(d);

            return _Source.GetAreaSample(a, b, c, d);
        }

        public Vector4 GetPointSample(UV uv)
        {
            uv = Transform(uv);

            return _Source.GetPointSample(uv);
        }

        protected abstract UV Transform(UV uv);        

        #endregion
    }

    sealed class _PolarTransformedTextureSampler : _TransformedTextureSampler
    {
        #region lifecycle

        public _PolarTransformedTextureSampler(ITextureSampler source) : base(source)
        {
            _Scale = source.Scale;
            _Center = _Scale / 2;
        }

        #endregion       

        #region API

        private readonly Vector2 _Scale;
        private readonly Vector2 _Center;

        protected override UV Transform(UV pp)
        {
            Vector2 p = pp;            

            p -= _Center; // offset coords to the center of the image

            var angle = -Math.Atan2(p.X, p.Y);
            angle += Math.PI;
            angle /= Math.PI * 2;

            p /= _Center; // normalize

            var radius = p.Length();

            return new Vector2((float)angle, 1 - (float)radius) * _Scale;
        }

        #endregion
    }

    sealed class _PerlinNoiseTextureSampler : ITextureSampler
    {
        // TODO: pass a Black/White colors and interpolate based on noise value.

        public static _PerlinNoiseTextureSampler Create(IPixel odd, IPixel even, int repeat, int octaves, float persistence, int randomSeed = 177)
        {
            return new _PerlinNoiseTextureSampler(odd.ToVector4(), even.ToVector4(), repeat, octaves, persistence, randomSeed);
        }

        private _PerlinNoiseTextureSampler(Vector4 odd, Vector4 even, int repeat, int octaves, float persistence, int randomSeed = 177)
        {
            if (repeat <= 0) repeat = -1; // repetition is disabled

            _Scale = repeat > 0 ? new SizeF(repeat, repeat) : new SizeF(1,1);            

            _Perlin = new Perlin_Tileable(randomSeed, repeat);

            _Depth = 0;
            _Octaves = octaves;
            _Persistence = persistence;            

            _OddColor = odd;
            _EvenColor = even;
        }

        private readonly Perlin_Tileable _Perlin;

        private readonly SizeF _Scale;

        private readonly float _Depth;
        private readonly int _Octaves;
        private readonly float _Persistence;
        private readonly Vector4 _OddColor;
        private readonly Vector4 _EvenColor;

        public SizeF Scale => _Scale;

        public Vector4 GetAreaSample(UV tl, UV tr, UV br, UV bl)
        {
            throw new NotImplementedException();
        }

        public Vector4 GetPointSample(UV uv)
        {
            var v = (float)_Perlin
                .OctavePerlin(uv.X, uv.Y, _Depth, _Octaves, _Persistence)
                .Clamp(0,1);

            return Vector4.Lerp(_OddColor, _EvenColor, v);
        }
    }
}
