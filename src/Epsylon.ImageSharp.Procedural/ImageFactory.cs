using System;
using System.Collections.Generic;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;

namespace Epsylon.ImageSharp.Procedural
{
    public static class ImageFactory
    {
        #region factory

        private static Configuration _CreateFactoryCfg()
        {
            var cfg = Configuration.Default.ShallowCopy();

            cfg.MemoryManager = new SimpleGcMemoryManager();

            #if DEBUG
            Configuration.Default.MemoryManager = null; // invalidate default configuration to signal inproper calls
            #endif

            return cfg;
        }

        private static readonly Configuration _DefaultConfiguration = _CreateFactoryCfg();        

        public static Image<TPixel> CreateImage<TPixel>(this Size size) where TPixel : struct, IPixel<TPixel>
        {
            if (size.Area() <= 0) throw new ArgumentException(nameof(size));

            return new Image<TPixel>(_DefaultConfiguration, size.Width, size.Height);
        }

        public static Image<TPixel> CreateImage<TPixel>(this Size size, TPixel[] data) where TPixel : struct, IPixel<TPixel>
        {
            if (size.Area() <= 0) throw new ArgumentException(nameof(size));
            if (data == null) throw new ArgumentNullException(nameof(data));            
            if (data.Length < size.Area()) throw new ArgumentException(nameof(data));

            return Image.LoadPixelData(_DefaultConfiguration, data, size.Width,size.Height);
        }

        public static Image<TPixel> CreateImage<TPixel>(this TPixel[] data, int width, int height) where TPixel : struct, IPixel<TPixel>
        {
            return new Size(width, height).CreateImage(data);
        }

        #endregion

        #region extensions

        public static Byte[] SaveAsBytes<TPixel>(this Image<TPixel> image, IImageEncoder encoder) where TPixel : struct, IPixel<TPixel>
        {
            using (var s = new System.IO.MemoryStream())
            {
                image.Save(s, encoder);
                s.Flush();
                return s.ToArray();
            }
        }

        #endregion
    }
}
