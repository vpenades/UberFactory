using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.MetaData;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Epsylon.ImageSharp.Procedural
{

    public interface ITexture : IImage
    {
        Vector4 this[int x, int y] { get; set; }
    }

    public interface ITextureSampler : ITexture
    {
        Vector4 GetPointSample(Vector2 uv);
        
        Vector4 GetAreaSample(Vector2 tl, Vector2 tr, Vector2 br, Vector2 bl);
    }


    class _ImageTexture<TPixel> : ITextureSampler, IDisposable where TPixel : struct, IPixel<TPixel>
    {
        #region lifecycle

        public _ImageTexture(Image<TPixel> source, bool leaveUndisposed = false)
        {
            _Source = source;
            _LeaveUndisposed = leaveUndisposed;

            _AddressU = SamplerAddressMode.Clamp.GetFunction(_Source.Width);
            _AddressV = SamplerAddressMode.Clamp.GetFunction(_Source.Height);
        }

        public void Dispose()
        {
            if (_Source != null && !_LeaveUndisposed)
            {
                _Source.Dispose();
                _Source = null;
            }
        }

        #endregion

        #region data

        private static readonly Vector2 V2HALF = Vector2.One * 0.5f;

        private Image<TPixel> _Source;
        private bool _LeaveUndisposed;

        private Func<int,int> _AddressU;
        private Func<int,int> _AddressV;

        #endregion

        #region properties

        public PixelTypeInfo PixelType => _Source.PixelType;

        public int Width => _Source.Width;

        public int Height => _Source.Height;

        public ImageMetaData MetaData => _Source.MetaData;

        public Vector4 this[int x, int y]
        {
            get
            {
                x = _AddressU(x);
                y = _AddressV(y);

                return _Source[x, y].ToVector4();
            }
            set
            {
                var c = default(TPixel);
                c.PackFromVector4(value);
                _Source[x, y] = c;
            }
        }

        #endregion

        #region API

        public Vector4 GetPointSample(Vector2 uv)
        {
            uv -= V2HALF;

            int ix = (int)(uv.X >= 0 ? uv.X : uv.X - 1);
            int iy = (int)(uv.Y >= 0 ? uv.Y : uv.Y - 1);

            ix = _AddressU(ix);
            iy = _AddressV(iy);

            var jx = _AddressU(ix + 1);
            var jy = _AddressV(iy + 1);

            var A = this[ix, iy];
            var B = this[jx, iy];
            var C = this[ix, jy];
            var D = this[jx, jy];

            uv.X -= ix;
            uv.Y -= iy;

            A = A.AlphaAwareLerp(B, uv.X); // first row
            C = C.AlphaAwareLerp(D, uv.X); // second row
            A = A.AlphaAwareLerp(C, uv.Y); // column

            return A;
        }

        public Vector4 GetAreaSample(Vector2 tl, Vector2 tr, Vector2 br, Vector2 bl)
        {
            // TODO: a more precise implementation would traverse a tree of mipmapped/anisotropic images until the area is ~1 and do point sample

            var p = (tl + tr + br + bl) / 4;

            return GetPointSample(p);
        }

        #endregion
    }

    class _ImageMaskedTexture<TPixel> : ITexture, IDisposable where TPixel : struct, IPixel<TPixel>
    {
        public _ImageMaskedTexture(Image<TPixel> color, Image<Alpha8> mask)
        {
            _Color = color;
            _Mask = mask;

            _AddressU = SamplerAddressMode.Clamp.GetFunction(_Color.Width);
            _AddressV = SamplerAddressMode.Clamp.GetFunction(_Color.Height);
        }

        public void Dispose()
        {
            if (_Color != null) { _Color.Dispose(); _Color = null; }
            if (_Mask != null) { _Mask.Dispose(); _Mask = null; }
        }

        private Image<TPixel> _Color;
        private Image<Alpha8> _Mask;
        private Func<int, int> _AddressU;
        private Func<int, int> _AddressV;

        public PixelTypeInfo PixelType => _Color.PixelType;

        public int Width => _Color.Width;

        public int Height => _Color.Height;

        public ImageMetaData MetaData => _Color.MetaData;

        public System.Numerics.Vector4 this[int x, int y]
        {
            get
            {
                x = _AddressU(x);
                y = _AddressV(y);

                var alpha = _Mask[x, y].ToVector4().W;

                return _Color[x, y].ToVector4().WithW( alpha ) ;
            }
            set
            {
                _Mask[x, y] = new Alpha8(value.W);

                value.W = _Color[x, y].ToVector4().W;

                var c = default(TPixel);
                c.PackFromVector4(value);
                _Color[x, y] = c;
            }
        }
    }
    
    class _PolarTransformedTexture : ITextureSampler
    {
        #region lifecycle

        public _PolarTransformedTexture(ITextureSampler source)
        {
            _Source = source;
        }

        #endregion

        #region data

        private readonly ITextureSampler _Source;

        private static readonly Vector2 _HALF = Vector2.One / 2;

        #endregion

        #region properties

        public PixelTypeInfo PixelType => _Source.PixelType;

        public int Width => _Source.Width;

        public int Height => _Source.Height;

        public ImageMetaData MetaData => _Source.MetaData;

        #endregion

        #region API

        public Vector4 this[int x, int y]
        {
            get => GetPointSample(new Vector2(x, y));
            set => throw new NotImplementedException();
        }

        public Vector4 GetAreaSample(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            a = _PolarTransform(a);
            b = _PolarTransform(b);
            c = _PolarTransform(c);
            d = _PolarTransform(d);

            return _Source.GetAreaSample(a, b, c, d);
        }        

        public Vector4 GetPointSample(Vector2 uv)
        {
            uv = _PolarTransform(uv);

            return _Source.GetPointSample(uv);
        }

        private Vector2 _PolarTransform(Vector2 p)
        {
            p -= _HALF; // offset coords to the center of the image

            var angle = -Math.Atan2(p.X, p.Y);
            angle += Math.PI;
            angle /= Math.PI * 2;

            p /= _HALF; // normalize

            var radius = p.Length();

            return new Vector2((float)angle, 1 - (float)radius);
        }

        #endregion
    }
}
