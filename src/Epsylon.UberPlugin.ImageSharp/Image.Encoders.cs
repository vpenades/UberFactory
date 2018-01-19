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

    public class EncoderAgent
    {
        public EncoderAgent(String ext, SixLabors.ImageSharp.Formats.IImageEncoder encoder)
        {
            Extension = ext;
            Encoder = encoder;
        }

        public String Extension { get; private set; }

        public SixLabors.ImageSharp.Formats.IImageEncoder Encoder { get; private set; }

        public void WriteImage(IMAGE32 image, System.IO.Stream stream)
        {
            image.Save(stream, Encoder);
        }

        public void WriteImage(IMAGE32 image, SDK.ExportContext ctx)
        {
            ctx.WriteStream(s => WriteImage(image, s));
        }        

        public Byte[] ToBytes(IMAGE32 image)
        {
            using (var s = new System.IO.MemoryStream())
            {
                image.Save(s, Encoder);
                s.Flush();
                return s.ToArray();
            }
        }
    }

    

    public abstract class EncoderBase : SDK.ContentFilter<EncoderAgent>
    {
        [SDK.Group(0)]
        [SDK.InputValue("IgnoreMetadata")]
        [SDK.Title("Ignore Metadata")]        
        public Boolean IgnoreMetadata { get; set; }
    }

    [SDK.Icon("◂PNG▸"),SDK.Title("PNG"), SDK.TitleFormat("PNG {0}")]
    [SDK.ContentNode("PngEncoder")]   
    
    public sealed class PngEncoder : EncoderBase
    {
        [SDK.Group(0)]
        [SDK.InputValue("ColorChannels")]
        [SDK.Title("Channels")]
        [SDK.Default(PNGFORMAT.PngColorType.RgbWithAlpha)]
        public PNGFORMAT.PngColorType ColorChannels { get; set; }

        [SDK.Group(0)]
        [SDK.InputValue("Quantizer")]
        [SDK.Title("Quantizer")]
        [SDK.Default(Quantization.Palette)]
        public Quantization Quantizer { get; set; }

        protected override EncoderAgent Evaluate()
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

            return new EncoderAgent("PNG",encoder);
        }
    }

    [SDK.Icon("◂JPEG▸"), SDK.Title("JPEG (Advanced)"),SDK.TitleFormat("JPEG {0}")]
    [SDK.ContentNode("JpegEncoderAdvanced")]
    public sealed class JpegEncoderAdvanced : EncoderBase
    {
        [SDK.Group(0)]
        [SDK.InputValue("Quality")]
        [SDK.Minimum(0)]
        [SDK.Default(80)]
        [SDK.Maximum( 100)]
        [SDK.ViewStyle("Slider")]
        public int Quality { get; set; }

        protected override EncoderAgent Evaluate()
        {
            var encoder = new JPGFORMAT.JpegEncoder
            {
                Quality = Quality
            };

            return new EncoderAgent("JPG", encoder);
        }
    }

    [SDK.Icon("◂JPEG▸"), SDK.Title("JPEG"), SDK.TitleFormat("JPEG {0}")]
    [SDK.ContentNode("JpegEncoderBasic")]    
    public sealed class JpegEncoderBasic : EncoderBase
    {
        protected override EncoderAgent Evaluate()
        {
            var settings = this.GetSharedSettings<JpegGlobalSettings>();

            var encoder = new JPGFORMAT.JpegEncoder
            {
                IgnoreMetadata = this.IgnoreMetadata,
                Quality = settings.Quality
            };

            return new EncoderAgent("JPG", encoder);
        }
    }

    [SDK.Icon("◂BMP▸"), SDK.Title("BMP"), SDK.TitleFormat("BMP {0}")]
    [SDK.ContentNode("BmpEncoder")]    
    public sealed class BmpEncoder : EncoderBase
    {
        [SDK.Group(0)]
        [SDK.InputValue("BitsPerPixel")]
        [SDK.Title("Bits Per Pixel")]
        public BMPFORMAT.BmpBitsPerPixel BitsPerPixel { get; set; }

        protected override EncoderAgent Evaluate()
        {
            var encoder = new BMPFORMAT.BmpEncoder
            {                
                BitsPerPixel = this.BitsPerPixel
            };

            return new EncoderAgent("BMP", encoder);
        }
    }

    [SDK.Icon("◂GIF▸"), SDK.Title("GIF"), SDK.TitleFormat("GIF {0}")]
    [SDK.ContentNode("GifEncoder")]    
    public sealed class GifEncoder : EncoderBase
    {
        [SDK.Group(0)]
        [SDK.InputValue("TransparencyThreshold")]
        [SDK.Title("Transparency Threshold")]
        [SDK.Minimum(0)]
        [SDK.Default(0)]
        [SDK.Maximum( 255)]
        public int TransparencyThreshold { get; set; }

        [SDK.Group(0)]
        [SDK.InputValue("Quantizer")]
        [SDK.Title("Quantizer")]
        [SDK.Default(Quantization.Palette)]
        public Quantization Quantizer { get; set; }

        protected override EncoderAgent Evaluate()
        {
            var encoder = new GIFFORMAT.GifEncoder
            {
                IgnoreMetadata = this.IgnoreMetadata,
                Threshold = (Byte)TransparencyThreshold,
                Quantizer = this.Quantizer.GetInstance()
            };

            return new EncoderAgent("GIF", encoder);
        }
    }    
}
