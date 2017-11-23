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
    [SDK.ContentMetaData("Title", "PNG Encoder")]
    [SDK.ContentMetaData("TitleFormat", "PNG {0}")]
    public sealed class PngEncoder : SDK.ContentFilter<IMAGEENCODER>
    {
        [SDK.InputValue("CompressionLevel")]
        [SDK.InputMetaData("Title", "Compression Level")]
        [SDK.InputMetaData("Minimum", 1)]
        [SDK.InputMetaData("Default", 6)]
        [SDK.InputMetaData("Maximum", 9)]
        [SDK.InputMetaData("ViewStyle", "Slider")]
        public int CompressionLevel { get; set; }

        [SDK.InputValue("ColorChannels")]
        [SDK.InputMetaData("Title", "Color Channels")]
        [SDK.InputMetaData("Default", PNGFORMAT.PngColorType.RgbWithAlpha)]
        public PNGFORMAT.PngColorType ColorChannels { get; set; }

        protected override IMAGEENCODER Evaluate()
        {
            var encoder = new PNGFORMAT.PngEncoder
            {
                Quantizer = null,
                CompressionLevel = CompressionLevel,
                PngColorType = ColorChannels
            };

            return ImageWriter.CreateEncoder(encoder, "PNG");
        }
    }

    [SDK.ContentNode("JpegEncoder")]
    [SDK.ContentMetaData("Title", "JPG Encoder")]
    [SDK.ContentMetaData("TitleFormat", "JPG {0}")]
    public sealed class JpegEncoder : SDK.ContentFilter<IMAGEENCODER>
    {
        [SDK.InputValue("Quality")]
        [SDK.InputMetaData("Minimum", 0)]
        [SDK.InputMetaData("Default", 80)]
        [SDK.InputMetaData("Maximum", 100)]
        [SDK.InputMetaData("ViewStyle", "Slider")]
        public int Quality { get; set; }

        protected override IMAGEENCODER Evaluate()
        {
            var encoder = new JPGFORMAT.JpegEncoder
            {
                Quality = Quality
            };

            return ImageWriter.CreateEncoder(encoder,"JPG");
        }
    }

    [SDK.ContentNode("BmpEncoder")]
    [SDK.ContentMetaData("Title", "BMP Encoder")]
    [SDK.ContentMetaData("TitleFormat", "BMP {0}")]
    public sealed class BmpEncoder : SDK.ContentFilter<IMAGEENCODER>
    {
        [SDK.InputValue("BitsPerPixel")]
        [SDK.InputMetaData("Title", "Bits Per Pixel")]
        public BMPFORMAT.BmpBitsPerPixel BitsPerPixel { get; set; }

        protected override IMAGEENCODER Evaluate()
        {
            var encoder = new BMPFORMAT.BmpEncoder
            {
                BitsPerPixel = this.BitsPerPixel
            };

            return ImageWriter.CreateEncoder(encoder,"BMP");
        }
    }


    [SDK.ContentNode("GifEncoder")]
    [SDK.ContentMetaData("Title", "GIF Encoder")]
    [SDK.ContentMetaData("TitleFormat", "GIF {0}")]
    public sealed class GifEncoder : SDK.ContentFilter<IMAGEENCODER>
    {
        [SDK.InputValue("TransparencyThreshold")]
        [SDK.InputMetaData("Title", "Transparency Threshold")]
        [SDK.InputMetaData("Minimum", 0)]
        [SDK.InputMetaData("Default", 0)]
        [SDK.InputMetaData("Maximum", 255)]
        public int TransparencyThreshold { get; set; }

        protected override IMAGEENCODER Evaluate()
        {
            var encoder = new GIFFORMAT.GifEncoder
            {
                Threshold = (Byte)TransparencyThreshold
            };

            return ImageWriter.CreateEncoder(encoder, "GIF");
        }
    }

    
}
