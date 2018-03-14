using System;
using System.Collections.Generic;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;

namespace Epsylon.ImageSharp.Procedural
{
    public static class ImageFactory
    {
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
            if (size.Area() <= 0) return null;

            return new Image<TPixel>(_DefaultConfiguration, size.Width, size.Height);
        }

        public static Image<TPixel> CreateImage<TPixel>(this TPixel[] data, Size size) where TPixel : struct, IPixel<TPixel>
        {
            if (data == null) return null;
            if (size.Area() <= 0) return null;

            return Image.LoadPixelData(_DefaultConfiguration, data, size.Width,size.Height);
        }

        public static Image<TPixel> CreateImage<TPixel>(this TPixel[] data, int width, int height) where TPixel : struct, IPixel<TPixel>
        {
            return data.CreateImage(new Size(width, height));
        }
    }
}
