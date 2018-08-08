using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.MetaData;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Advanced;
using SixLabors.Primitives;


namespace Epsylon.ImageSharp.Procedural
{
    public enum AddressMode
    {
        Wrap = 1,
        Mirror = 2,
        Clamp = 3,
        // Border = 4, // this has been deprecated in most hardware implementations since it requires an extra register
        MirrorOnce = 5
    }

    public interface IBitmap<TPixel> where TPixel : struct, IPixel<TPixel>
    {
        int Width { get; }
        int Height { get; }

        TPixel this[int x, int y] { get; set; }
    }

    public interface IBitmapSampler
    {
        int Width { get; }
        int Height { get; }

        Vector4 this[int x, int y] { get; set; }
    }

    public static class BitmapFactory
    {
        public static Size Size<TPixel>(this IBitmap<TPixel> bitmap) where TPixel : struct, IPixel<TPixel>
        {
            return new Size(bitmap.Width, bitmap.Height);
        }

        public static Rectangle Bounds<TPixel>(this IBitmap<TPixel> bitmap) where TPixel : struct, IPixel<TPixel>
        {
            return new Rectangle(0,0,bitmap.Width, bitmap.Height);
        }

        public static IBitmap<TPixel> ToBitmap<TPixel>(this Image<TPixel> image) where TPixel : struct, IPixel<TPixel>
        {
            return _Bitmap<TPixel>.Create(image);
        }

        public static IBitmap<Rgba32> Create(int width, int height)
        {
            return _Bitmap<Rgba32>.Create(width, height);
        }

        public static IBitmap<Rgba32> LoadFromFile(string filePath)
        {
            using (var img = Image.Load(filePath))
            {
                return _Bitmap<Rgba32>.Create(img);
            }
        }

        public static IBitmap<Rgba32> LoadFromBytes(Byte[] content)
        {
            using (var img = Image.Load(content))
            {
                return _Bitmap<Rgba32>.Create(img);
            }
        }

        public static IBitmap<Rgba32> LoadFromStream(System.IO.Stream stream)
        {
            using (var img = Image.Load(stream))
            {
                return _Bitmap<Rgba32>.Create(img);
            }
        }

        public static Image<Rgba32> ToImageSharp(this IBitmap<Rgba32> bitmap)
        {
            return _Bitmap<Rgba32>._CastOrCopy(bitmap)?._ToImageSharp();
        }

        public static void Save(this IBitmap<Rgba32> bitmap, string filePath)
        {
            using (var img = bitmap.ToImageSharp())
            {
                img.Save(filePath);
            }
        }

        public static void Save(this IBitmap<Rgba32> bitmap, System.IO.Stream stream, IImageEncoder encoder)
        {
            using (var img = bitmap.ToImageSharp())
            {
                img.Save(stream,encoder);
            }
        }

        public static IBitmap<TPixel> WithWrapMode<TPixel>(this IBitmap<TPixel> bitmap, AddressMode u, AddressMode v) where TPixel : struct, IPixel<TPixel>
        {
            // todo: if bitmap is already a _WrapBitmap, set values

            return new _WrapBitmap<TPixel>(bitmap,u,v);
        }

        public static IBitmapSampler ToPixelSampler<TPixel>(this Image<TPixel> source) where TPixel : struct, IPixel<TPixel>
        {
            return source.ToBitmap().ToPixelSampler();
        }

        public static IBitmapSampler ToPixelSampler<TPixel>(this IBitmap<TPixel> source) where TPixel : struct, IPixel<TPixel>
        {
            if (source is IBitmapSampler sampler) return sampler;

            throw new NotImplementedException(); // TODO create a wrapper
        }

        public static IBitmapSampler ToPixelSampler<TPixel>(this Image<TPixel> source, AddressMode u, AddressMode v) where TPixel : struct, IPixel<TPixel>
        {
            return source.ToBitmap().WithWrapMode(u, v).ToPixelSampler();
        }
    }

    
    
    /// <summary>
    /// GC friendly Image
    /// </summary>
    /// <remarks>
    /// ImageSharp.Image<T> implements IDisposable, that enforces all owners to also be disposable so we can properly dipose resources.
    /// 
    /// For complex scenarios with a tree graph of cross referenced objects, it's difficult to track who owns what and what
    /// needs to be disposed and in some cases might require manual reference counting, which is difficult to maintain, and
    /// counterintuitive to the Managed World.
    /// 
    /// This class allows creating images in scenarios without worrying about tracking ownership and disposal, since the
    /// internal buffer is fully handled by the GC
    /// 
    /// Renamed to Bitmap to better distinguish with ImageSharp's Image
    /// </remarks>
    /// <typeparam name="TPixel"></typeparam>
    sealed class _Bitmap<TPixel> : IBitmap<TPixel>, IBitmapSampler where TPixel : struct, IPixel<TPixel>
    {
        #region lifecycle

        internal static _Bitmap<TPixel> _CastOrCopy(IBitmap<TPixel> bitmap)
        {
            if (bitmap == null) return null;
            if (bitmap is _Bitmap<TPixel> instance) return instance;
            throw new NotImplementedException(); // TODO: create a copy
        }

        public static _Bitmap<TPixel> Create(int w, int h)
        {
            if (w <= 0 || h <= 0) return null;

            return new _Bitmap<TPixel>(w, h);
        }

        public static _Bitmap<TPixel> Create(Image<TPixel> image)
        {
            if (image == null) return null;

            return new _Bitmap<TPixel>(image);
        }

        private _Bitmap(Image<TPixel> image) : this(image.Width,image.Height)
        {
            image
                .GetPixelSpan()
                .CopyTo(_Buffer);
        }

        private _Bitmap(int width, int height)
        {
            _Width = width;
            _Height = height;

            var size = width * height;

            _Buffer = new TPixel[size];

            //_MetaData = new ImageMetaData(); // can't do this yet
        }

        public _Bitmap<TPixel> Clone()
        {
            var cloned = new _Bitmap<TPixel>(_Width, _Height);
            _Buffer.CopyTo(cloned._Buffer, 0);

            // cloned._MetaData = this._MetaData.Clone();

            return cloned;
        }

        #endregion

        #region data

        private TPixel[] _Buffer;
        private int _Width;
        private int _Height;

        private ImageMetaData _MetaData;

        #endregion

        #region properties

        public TPixel this[int x, int y]
        {
            get { System.Diagnostics.Debug.Assert(Contains(x, y), "Out of bounds"); return _Buffer[y * _Width + x]; }
            set { System.Diagnostics.Debug.Assert(Contains(x, y), "Out of bounds"); _Buffer[y * _Width + x] = value; }
        }

        Vector4 IBitmapSampler.this[int x, int y]
        {
            get { System.Diagnostics.Debug.Assert(Contains(x, y), "Out of bounds"); return _Buffer[y * _Width + x].ToVector4(); }
            set { System.Diagnostics.Debug.Assert(Contains(x, y), "Out of bounds"); var c = default(TPixel); c.PackFromVector4(value); _Buffer[y * _Width + x] = c; }
        }        

        public int Width => _Width;

        public int Height => _Height;

        public ImageMetaData MetaData => _MetaData;        

        #endregion

        #region API

        public bool Contains(int x, int y)
        {
            if (x < 0 || x >= Width) return false;
            if (y < 0 || y >= Height) return false;
            return true;
        }

        internal Image<TPixel> _ToImageSharp()
        {
            return _Buffer.CreateImage(_Width, _Height);
        }

        public void Mutate(Action<IImageProcessingContext<TPixel>> operation)
        {
            // this is a hat trick to ensure disposable resources are short lived and always disposed.

            using (var image = _ToImageSharp()) // ImageSharp request: ideally, here the image should use the original buffer, without copying it.
            {
                // image.MetaData = _MetaData; // Can't do this yet                               

                image.Mutate(operation);

                _MetaData = image.MetaData;

                _Width = image.Width;
                _Height = image.Height;

                var newSize = _Width * _Height;

                if (newSize != _Buffer.Length) _Buffer = new TPixel[newSize];

                image
                    .GetPixelSpan()
                    .CopyTo(_Buffer); // ImageSharp request:  if _Buffer happens to be the same buffer stored internally, DO NOTHING
            }
        }        

        public _Bitmap<TPixel> Clone(Action<IImageProcessingContext<TPixel>> operation)
        {
            var clone = this.Clone();  clone.Mutate(operation);
            return clone;
        }        

        #endregion
    }

    sealed class _WrapBitmap<TPixel> : IBitmap<TPixel>, IBitmapSampler where TPixel : struct, IPixel<TPixel>
    {
        #region lifecycle

        public _WrapBitmap(IBitmap<TPixel> source, AddressMode u, AddressMode v)
        {
            _Bitmap = source;
            _Sampler = _Bitmap.ToPixelSampler();

            SetModeX(u);
            SetModeY(v);            
        }

        #endregion

        #region data

        private static readonly Vector2 V2HALF = Vector2.One * 0.5f;

        private IBitmap<TPixel> _Bitmap;
        private IBitmapSampler _Sampler;

        private Func<int, int> _AddressReadX;
        private Func<int, int> _AddressReadY;
        private Func<int, int> _AddressWriteX;
        private Func<int, int> _AddressWriteY;

        #endregion

        #region properties        

        public int Width => _Bitmap.Width;

        public int Height => _Bitmap.Height;
        
        #endregion

        #region API

        public void SetModeX(AddressMode mode)
        {
            _AddressReadX = _AddressWriteX = mode.GetFunction(_Bitmap.Width);
            if (mode == AddressMode.Clamp) _AddressWriteX = x => (x >= 0 && x < _Bitmap.Width) ? x : -1;
        }

        public void SetModeY(AddressMode mode)
        {
            _AddressReadY = _AddressWriteY = mode.GetFunction(_Bitmap.Height);
            if (mode == AddressMode.Clamp) _AddressWriteY = y => (y >= 0 && y < _Bitmap.Height) ? y : -1;
        }

        public TPixel this[int x, int y]
        {
            get
            {
                x = _AddressReadX(x);
                y = _AddressReadY(y);
                return _Bitmap[x, y];
            }
            set
            {
                x = _AddressWriteX(x); if (x < 0) return;
                y = _AddressWriteY(y); if (y < 0) return;
                _Bitmap[x, y] = value;
            }
        }

        Vector4 IBitmapSampler.this[int x, int y]
        {
            get
            {
                x = _AddressReadX(x);
                y = _AddressReadY(y);
                return _Sampler[x, y];
            }
            set
            {
                x = _AddressWriteX(x); if (x < 0) return;
                y = _AddressWriteY(y); if (y < 0) return;
                _Sampler[x, y] = value;
            }
        }

        #endregion
    }
}
