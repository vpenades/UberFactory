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

    public abstract class EncoderBase : SDK.ContentFilter<IMAGEENCODER>
    {
        [SDK.InputValue("IgnoreMetadata")]
        [SDK.Title("Ignore Metadata")]        
        public Boolean IgnoreMetadata { get; set; }
    }

    [SDK.ContentNode("PngEncoder")]
    [SDK.Title("PNG")]
    [SDK.TitleFormat( "PNG {0}")]
    public sealed class PngEncoder : EncoderBase
    {
        [SDK.InputValue("ColorChannels")]
        [SDK.Title("Color Channels")]
        [SDK.Default(PNGFORMAT.PngColorType.RgbWithAlpha)]
        public PNGFORMAT.PngColorType ColorChannels { get; set; }

        [SDK.InputValue("Quantizer")]
        [SDK.Title("Palette Quantizer")]
        [SDK.Default(Quantization.Palette)]
        public Quantization Quantizer { get; set; }

        protected override IMAGEENCODER Evaluate()
        {
            var settings = this.GetSharedSettings<PngGlobalSettings>();

            bool isPalette = ColorChannels == PNGFORMAT.PngColorType.Palette;

            var encoder = new PNGFORMAT.PngEncoder
            {
                IgnoreMetadata = this.IgnoreMetadata,
                PaletteSize = isPalette ? 255 : 0,
                Quantizer = isPalette ? Quantizer.GetInstance() : null,
                CompressionLevel = settings.CompressionLevel,
                PngColorType = ColorChannels
            };

            return ImageWriter.CreateEncoder(encoder, "PNG");
        }
    }

    [SDK.ContentNode("JpegEncoderAdvanced")]
    [SDK.Title("JPG (Advanced)")]
    [SDK.TitleFormat( "JPG {0}")]
    public sealed class JpegEncoderAdvanced : EncoderBase
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

            return ImageWriter.CreateEncoder(encoder,"JPG");
        }
    }

    [SDK.ContentNode("JpegEncoderBasic")]
    [SDK.Title("JPG")]
    [SDK.TitleFormat("JPG {0}")]
    public sealed class JpegEncoderBasic : EncoderBase
    {
        protected override IMAGEENCODER Evaluate()
        {
            var settings = this.GetSharedSettings<JpegGlobalSettings>();

            var encoder = new JPGFORMAT.JpegEncoder
            {
                IgnoreMetadata = this.IgnoreMetadata,
                Quality = settings.Quality
            };

            return ImageWriter.CreateEncoder(encoder, "JPG");
        }
    }

    [SDK.ContentNode("BmpEncoder")]
    [SDK.Title("BMP")]
    [SDK.TitleFormat( "BMP {0}")]
    public sealed class BmpEncoder : EncoderBase
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

            return ImageWriter.CreateEncoder(encoder,"BMP");
        }
    }


    [SDK.ContentNode("GifEncoder")]
    [SDK.Title("GIF")]
    [SDK.TitleFormat( "GIF {0}")]
    public sealed class GifEncoder : EncoderBase
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
                IgnoreMetadata = this.IgnoreMetadata,
                Threshold = (Byte)TransparencyThreshold
            };

            return ImageWriter.CreateEncoder(encoder, "GIF");
        }
    }

    
}
