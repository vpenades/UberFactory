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
        /*
        private class _InvalidMemoryManager : MemoryManager
        {
            internal override IBuffer<T> Allocate<T>(int length, bool clear)
            {
                throw new NotSupportedException();
            }

            internal override IManagedByteBuffer AllocateManagedByteBuffer(int length, bool clear)
            {
                throw new NotSupportedException();
            }
        }*/

        private static Configuration _CreateFactoryCfg()
        {
            var cfg = Configuration.Default; // .ShallowCopy();

            // register custom formats            

            // TODO: create an Invalid memory manager and set it as the default.

            return cfg;
        }

        private static readonly Configuration _DefaultConfiguration = _CreateFactoryCfg();        

        public static Image<TPixel> CreateImage<TPixel>(this Size size) where TPixel : struct, IPixel<TPixel>
        {
            if (size.Width <= 0 || size.Height <= 0) return null;

            return new Image<TPixel>(_DefaultConfiguration, size.Width, size.Height);
        }

        public static Image<TPixel> CreateImage<TPixel>(this TPixel[] data, Size size) where TPixel : struct, IPixel<TPixel>
        {
            if (data == null) return null;
            if (size.Width <= 0 || size.Height <= 0) return null;

            return Image.LoadPixelData(_DefaultConfiguration, data, size.Width,size.Height);
        }

        public static Image<TPixel> CreateImage<TPixel>(this TPixel[] data, int width, int height) where TPixel : struct, IPixel<TPixel>
        {
            return data.CreateImage(new Size(width, height));
        }
    }
}
