using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.MetaData;
using SixLabors.ImageSharp.PixelFormats;

namespace Epsylon.ImageSharp.Procedural
{
    public enum SamplerAddressMode
    {
        Wrap = 1,
        Mirror = 2,
        Clamp = 3,
        // Border = 4, // this has been deprecated in most hardware implementations since it requires an extra register
        MirrorOnce = 5
    }    

    public interface IPixelSampler
    {
        int Width { get; }
        int Height { get; }

        Vector4 this[int x, int y] { get; set; }
    }

    public static class PixelSamplerFactory
    {
        public static IPixelSampler ToPixelSampler<TPixel>(this Image<TPixel> source) where TPixel : struct, IPixel<TPixel>
        {
            return new _UnmanagedPixelSampler<TPixel>(source, SamplerAddressMode.Clamp, SamplerAddressMode.Clamp, true);
        }

        public static IPixelSampler ToPixelSampler<TPixel>(this Image<TPixel> source, SamplerAddressMode u, SamplerAddressMode v) where TPixel : struct, IPixel<TPixel>
        {
            return new _UnmanagedPixelSampler<TPixel>(source, u,v,true);
        }        

        public static IPixelSampler ToPixelSampler<TPixel>(this Image<TPixel> source, Image<Alpha8> mask) where TPixel : struct, IPixel<TPixel>
        {
            return new _MaskedPixelSampler<TPixel>(source, mask);
        }
    }


    class _WrapSampler : IPixelSampler // TODO: generate all combinations with a TT
    {
        #region lifecycle

        public _WrapSampler(IPixelSampler source, SamplerAddressMode u, SamplerAddressMode v)
        {
            _Source = source;

            _AddressReadU = _AddressWriteU = u.GetFunction(_Source.Width);
            _AddressReadV = _AddressWriteV = v.GetFunction(_Source.Height);

            if (u == SamplerAddressMode.Clamp) _AddressWriteU = x => (x >= 0 && x < _Source.Width) ? x : -1;
            if (v == SamplerAddressMode.Clamp) _AddressWriteV = y => (y >= 0 && y < _Source.Height) ? y : -1;
        }

        #endregion

        #region data

        private static readonly Vector2 V2HALF = Vector2.One * 0.5f;

        private IPixelSampler _Source;        

        private Func<int, int> _AddressReadU;
        private Func<int, int> _AddressReadV;
        private Func<int, int> _AddressWriteU;
        private Func<int, int> _AddressWriteV;

        #endregion

        #region properties

        // public PixelTypeInfo PixelType => _Source.PixelType;

        public int Width => _Source.Width;

        public int Height => _Source.Height;

        // public ImageMetaData MetaData => _Source.MetaData;

        #endregion

        #region API

        public Vector4 this[int x, int y]
        {
            get
            {
                x = _AddressReadU(x);
                y = _AddressReadV(y);
                return _Source[x, y];
            }
            set
            {
                x = _AddressWriteU(x); if (x < 0) return;
                y = _AddressWriteV(y); if (y < 0) return;
                _Source[x, y] = value;
            }
        }

        #endregion
    }

    class _UnmanagedPixelSampler<TPixel> : IPixelSampler, IDisposable where TPixel : struct, IPixel<TPixel>
    {
        #region lifecycle

        public _UnmanagedPixelSampler(Image<TPixel> source, SamplerAddressMode u, SamplerAddressMode v, bool leaveUndisposed = false)
        {
            _Source = source;
            _LeaveUndisposed = leaveUndisposed;

            _AddressU = u.GetFunction(_Source.Width);
            _AddressV = v.GetFunction(_Source.Height);
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

        private Func<int, int> _AddressU;
        private Func<int, int> _AddressV;

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
    }


    



    class _MaskedPixelSampler<TPixel> : IPixelSampler, IDisposable where TPixel : struct, IPixel<TPixel>
    {
        public _MaskedPixelSampler(Image<TPixel> color, Image<Alpha8> mask)
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

                return _Color[x, y].ToVector4().WithW(alpha);
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
}
