using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.MetaData;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.ImageSharp.Procedural
{
    public interface ITexture : IImage
    {
        System.Numerics.Vector4 this[int x, int y] { get; set; }
    }


    class _ImageTexture<TPixel> : ITexture, IDisposable where TPixel : struct, IPixel<TPixel>
    {
        public _ImageTexture(Image<TPixel> source)
        {
            _Source = source;

            _AddressU = SamplerAddressMode.Clamp.GetFunction(_Source.Width);
            _AddressV = SamplerAddressMode.Clamp.GetFunction(_Source.Height);
        }

        public void Dispose()
        {
            if (_Source != null)
            {
                _Source.Dispose();
                _Source = null;
            }
        }

        private Image<TPixel> _Source;
        private Func<int,int> _AddressU;
        private Func<int,int> _AddressV;

        public PixelTypeInfo PixelType => _Source.PixelType;

        public int Width => _Source.Width;

        public int Height => _Source.Height;

        public ImageMetaData MetaData => _Source.MetaData;

        public System.Numerics.Vector4 this[int x, int y]
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
}
