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
    public class ManagedImage<TPixel> : IImage where TPixel : struct, IPixel<TPixel>
    {
        #region lifecycle

        public ManagedImage(int width, int height)
        {
            _Width = width;
            _Height = height;

            var size = width * height * System.Runtime.InteropServices.Marshal.SizeOf(typeof(TPixel));

            _Buffer = new Byte[size];
        }

        #endregion

        #region data

        private Byte[] _Buffer;
        private int _Width;
        private int _Height;

        #endregion

        #region properties

        public Vector4 this[int x, int y]
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public PixelTypeInfo PixelType => throw new NotImplementedException();

        public int Width => _Width;

        public int Height => _Height;

        public ImageMetaData MetaData => throw new NotImplementedException();

        #endregion

        #region API

        public void Mutate(Action<IImageProcessingContext<TPixel>> operation)
        {
            // this is a hat trick to ensure disposable resources are short lived and always disposed.

            using (var image = Image.LoadPixelData<TPixel>(_Buffer, _Width, _Height))
            {
                // image.MetaData = _MetaData; // Can't do this yet                               

                image.Mutate(operation);

                _Width = image.Width;
                _Height = image.Height;                

                var newSize = _Width * _Height * System.Runtime.InteropServices.Marshal.SizeOf(typeof(TPixel));

                if (newSize != _Buffer.Length) _Buffer = new byte[newSize];

                image.SavePixelData(_Buffer);
            }
        }

        #endregion
    }
}
