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
    public interface IBitmap<TPixel> where TPixel : struct, IPixel<TPixel>
    {
        int Width { get; }
        int Height { get; }

        TPixel this[int x, int y] { get; set; }
    }

    /// <summary>
    /// GC friendly Image
    /// </summary>
    /// <remarks>
    /// Image<T> implements IDisposable, that enforces all owners to also be disposable so we can properly dipose resources.
    /// 
    /// For complex scenarios with a tree graph of cross referenced objects, it's difficult to track who owns what and what
    /// needs to be disposed and in some cases might require manual reference counting, which is difficult to maintain, and
    /// counterintuitive to the Managed World.
    /// 
    /// This class allows creating images in scenarios without worrying about tracking ownership and disposal, since the
    /// internal buffer is fully handled by the GC
    /// </remarks>
    /// <typeparam name="TPixel"></typeparam>
    public class Bitmap<TPixel> : IBitmap<TPixel>, IPixelSampler where TPixel : struct, IPixel<TPixel>
    {
        #region lifecycle        

        public static Bitmap<TPixel> Create(int w, int h)
        {
            if (w <= 0 || h <= 0) return null;

            return new Bitmap<TPixel>(w, h);
        }

        public static Bitmap<TPixel> Create(Image<TPixel> image)
        {
            if (image == null) return null;

            return new Bitmap<TPixel>(image);
        }

        private Bitmap(Image<TPixel> image) : this(image.Width,image.Height)
        {
            image.SavePixelData(_Buffer);
        }

        private Bitmap(int width, int height)
        {
            _Width = width;
            _Height = height;

            var size = width * height;

            _Buffer = new TPixel[size];

            //_MetaData = new ImageMetaData(); // can't do this yet
        }

        public Bitmap<TPixel> Clone()
        {
            var cloned = new Bitmap<TPixel>(_Width, _Height);
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
            get => _Buffer[y * _Width + x];
            set => _Buffer[y * _Width + x] = value;
        }

        Vector4 IPixelSampler.this[int x, int y]
        {
            get => _Buffer[y * _Width + x].ToVector4();
            set { var c = default(TPixel); c.PackFromVector4(value); _Buffer[y * _Width + x] = c; }
        }

        public PixelTypeInfo PixelType => throw new NotImplementedException(); // Can't do this yet

        public int Width => _Width;

        public int Height => _Height;

        public ImageMetaData MetaData => _MetaData;        

        #endregion

        #region API

        public void Mutate(Action<IImageProcessingContext<TPixel>> operation)
        {
            // this is a hat trick to ensure disposable resources are short lived and always disposed.

            using (var image = Image.LoadPixelData<TPixel>(_Buffer, _Width, _Height)) // ImageSharp request: ideally, here the image should use the original buffer, without copying it.
            {
                // image.MetaData = _MetaData; // Can't do this yet                               

                image.Mutate(operation);

                _MetaData = image.MetaData;

                _Width = image.Width;
                _Height = image.Height;                

                var newSize = _Width * _Height;

                if (newSize != _Buffer.Length) _Buffer = new TPixel[newSize];

                image.SavePixelData(_Buffer); // ImageSharp request:  if _Buffer happens to be the same buffer stored internally, DO NOTHING
            }
        }

        public Bitmap<TPixel> Clone(Action<IImageProcessingContext<TPixel>> operation)
        {
            var clone = this.Clone();  clone.Mutate(operation);
            return clone;
        }

        #endregion
    }    
}
