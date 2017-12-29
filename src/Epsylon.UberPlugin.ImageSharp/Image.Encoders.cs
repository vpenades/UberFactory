using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SixLabors.ImageSharp;

using IMAGE32 = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.Rgba32>;

namespace Epsylon.UberPlugin
{
    using UberFactory;    

    using PNGFORMAT = SixLabors.ImageSharp.Formats.Png;
    using JPGFORMAT = SixLabors.ImageSharp.Formats.Jpeg;
    using BMPFORMAT = SixLabors.ImageSharp.Formats.Bmp;
    using GIFFORMAT = SixLabors.ImageSharp.Formats.Gif;    

    using IMAGEENCODER = KeyValuePair<string, Action<UberFactory.SDK.ExportContext, IMAGE32>>;

    [SDK.ContentNode("PngEncoder")]
    [SDK.Title("PNG Encoder")]
    [SDK.TitleFormat( "PNG {0}")]
    public sealed class PngEncoder : SDK.ContentFilter<IMAGEENCODER>
    {
        [SDK.InputValue("CompressionLevel")]
        [SDK.Title("Compression Level")]
        [SDK.Minimum(1)]
        [SDK.Default(6)]
        [SDK.Maximum( 9)]
        [SDK.ViewStyle("Slider")]
        public int CompressionLevel { get; set; }

        [SDK.InputValue("ColorChannels")]
        [SDK.Title("Color Channels")]
        [SDK.Default(PNGFORMAT.PngColorType.RgbWithAlpha)]
        public PNGFORMAT.PngColorType ColorChannels { get; set; }

        protected override IMAGEENCODER Evaluate()
        {
            var encoder = new PNGFORMAT.PngEncoder
            {
                Quantizer = null,
                CompressionLevel = CompressionLevel,
                PngColorType = ColorChannels
            };

            return ImageWriterAdvanced.CreateEncoder(encoder, "PNG");
        }
    }

    [SDK.ContentNode("JpegEncoder")]
    [SDK.Title("JPG Encoder")]
    [SDK.TitleFormat( "JPG {0}")]
    public sealed class JpegEncoder : SDK.ContentFilter<IMAGEENCODER>
    {
        [SDK.InputValue("Quality")]
        [SDK.Minimum(0)]
        [SDK.Default(80)]
        [SDK.Maximum( 100)]
        [SDK.ViewStyle("Slider")]
        public int Quality { get; set; }

        protected override IMAGEENCODER Evaluate()
        {
            var encoder = new JPGFORMAT.JpegEncoder
            {
                Quality = Quality
            };

            return ImageWriterAdvanced.CreateEncoder(encoder,"JPG");
        }
    }

    [SDK.ContentNode("BmpEncoder")]
    [SDK.Title("BMP Encoder")]
    [SDK.TitleFormat( "BMP {0}")]
    public sealed class BmpEncoder : SDK.ContentFilter<IMAGEENCODER>
    {
        [SDK.InputValue("BitsPerPixel")]
        [SDK.Title("Bits Per Pixel")]
        public BMPFORMAT.BmpBitsPerPixel BitsPerPixel { get; set; }

        protected override IMAGEENCODER Evaluate()
        {
            var encoder = new BMPFORMAT.BmpEncoder
            {
                BitsPerPixel = this.BitsPerPixel
            };

            return ImageWriterAdvanced.CreateEncoder(encoder,"BMP");
        }
    }


    [SDK.ContentNode("GifEncoder")]
    [SDK.Title("GIF Encoder")]
    [SDK.TitleFormat( "GIF {0}")]
    public sealed class GifEncoder : SDK.ContentFilter<IMAGEENCODER>
    {
        [SDK.InputValue("TransparencyThreshold")]
        [SDK.Title("Transparency Threshold")]
        [SDK.Minimum(0)]
        [SDK.Default(0)]
        [SDK.Maximum( 255)]
        public int TransparencyThreshold { get; set; }

        protected override IMAGEENCODER Evaluate()
        {
            var encoder = new GIFFORMAT.GifEncoder
            {
                Threshold = (Byte)TransparencyThreshold
            };

            return ImageWriterAdvanced.CreateEncoder(encoder, "GIF");
        }
    }

    
}
