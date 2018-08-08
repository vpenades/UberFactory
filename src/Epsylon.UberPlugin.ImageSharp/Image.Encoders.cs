using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;

namespace Epsylon.UberPlugin
{
    using UberFactory;
    using Epsylon.ImageSharp.Procedural;

    using IMAGE32 = Image<Rgba32>;

    using PNGFORMAT = SixLabors.ImageSharp.Formats.Png;
    using JPGFORMAT = SixLabors.ImageSharp.Formats.Jpeg;
    using BMPFORMAT = SixLabors.ImageSharp.Formats.Bmp;
    using GIFFORMAT = SixLabors.ImageSharp.Formats.Gif;
    
    [Flags]
    public enum AlphaEncoding
    {
        Default = 0,
        Premultiply = 1,
        EdgePadding = 2
    }

    public class EncoderAgent
    {
        #region lifecycle

        public EncoderAgent(String ext, IImageEncoder encoder, AlphaEncoding alpha = AlphaEncoding.Default)
        {
            Extension = ext;
            Encoder = encoder;
            _AlphaEncoding = alpha;
        }

        #endregion

        #region data

        private AlphaEncoding _AlphaEncoding;

        public String Extension { get; private set; }

        public IImageEncoder Encoder { get; private set; }

        #endregion

        #region core

        private IMAGE32 _PreProcess(IMAGE32 original)
        {
            if (_AlphaEncoding == AlphaEncoding.Default) return null;

            var processed = original.Clone();

            if (_AlphaEncoding.HasFlag(AlphaEncoding.EdgePadding) ) processed.Mutate(dc => dc.EdgePaddingAlpha(0));
            if (_AlphaEncoding.HasFlag(AlphaEncoding.Premultiply) ) processed.Mutate(dc => dc.PremultiplyAlpha());

            return processed;
        }

        public void WriteImage(IMAGE32 image, SDK.ExportContext ctx)
        {
            ctx.WriteStream(s => WriteImage(image, s));
        }

        public void WriteImage(IMAGE32 image, System.IO.Stream stream)
        {
            using (var processed = _PreProcess(image))
            {
                (processed ?? image).Save(stream, Encoder);
            }                
        }        

        public Byte[] ToBytes(IMAGE32 image)
        {
            using (var processed = _PreProcess(image))
            {
                return (processed ?? image).SaveAsBytes(Encoder);                    
            }
        }

        #endregion
    }
    
    

    public abstract class EncoderBase : SDK.ContentFilter<EncoderAgent>
    {
        [SDK.Group(0)]
        [SDK.InputValue("IgnoreMetadata")]
        [SDK.Title("Ignore Metadata")]        
        public Boolean IgnoreMetadata { get; set; }
    }

    [SDK.Icon("◂PNG▸"),SDK.Title("PNG"), SDK.TitleFormat("PNG {0}")]
    [SDK.ContentNode("PngEncoder")] public sealed class PngEncoder : EncoderBase
    {
        [SDK.Group(0), SDK.Title("Channels")]                
        [SDK.Default(PNGFORMAT.PngColorType.RgbWithAlpha)]
        [SDK.InputValue("ColorChannels")] public PNGFORMAT.PngColorType ColorChannels { get; set; }

        [SDK.Group(0), SDK.Title("Alpha Channel")]        
        [SDK.Default(AlphaEncoding.Default)]
        [SDK.InputValue("AlphaProcessing")] public AlphaEncoding AlphaProcessing { get; private set; }

        [SDK.Group(0), SDK.Title("Quantizer")]        
        [SDK.Default(Quantizer.Palette)]
        [SDK.InputValue("Quantizer")] public Quantizer Quantizer { get; set; }

        protected override EncoderAgent Evaluate()
        {
            var settings = this.GetSharedSettings<PngGlobalSettings>();

            bool isPalette = ColorChannels == PNGFORMAT.PngColorType.Palette;

            var encoder = new PNGFORMAT.PngEncoder
            {                
                Quantizer = isPalette ? Quantizer.GetInstance() : null,
                CompressionLevel = settings.CompressionLevel,
                ColorType = ColorChannels
            };

            if (ColorChannels != PNGFORMAT.PngColorType.RgbWithAlpha &&
                ColorChannels != PNGFORMAT.PngColorType.GrayscaleWithAlpha)
                AlphaProcessing = AlphaEncoding.Default;

            return new EncoderAgent("PNG",encoder, AlphaProcessing);
        }
    }

    [SDK.Icon("◂JPEG▸"), SDK.Title("JPEG (Advanced)"),SDK.TitleFormat("JPEG {0}")]
    [SDK.ContentNode("JpegEncoderAdvanced")] public sealed class JpegEncoderAdvanced : EncoderBase
    {
        [SDK.Group(0)]        
        [SDK.Minimum(0),SDK.Default(80),SDK.Maximum( 100)]
        [SDK.ViewStyle("Slider")]
        [SDK.InputValue("Quality")] public int Quality { get; set; }

        protected override EncoderAgent Evaluate()
        {
            var encoder = new JPGFORMAT.JpegEncoder
            {
                IgnoreMetadata = this.IgnoreMetadata,
                Quality = Quality
            };

            return new EncoderAgent("JPG", encoder);
        }
    }

    [SDK.Icon("◂JPEG▸"), SDK.Title("JPEG"), SDK.TitleFormat("JPEG {0}")]
    [SDK.ContentNode("JpegEncoderBasic")] public sealed class JpegEncoderBasic : EncoderBase
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
    [SDK.ContentNode("BmpEncoder")] public sealed class BmpEncoder : EncoderBase
    {
        [SDK.Group(0), SDK.Title("Bits Per Pixel")]
        [SDK.InputValue("BitsPerPixel")] public BMPFORMAT.BmpBitsPerPixel BitsPerPixel { get; set; }

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
    [SDK.ContentNode("GifEncoder")] public sealed class GifEncoder : EncoderBase
    {
        [SDK.Group(0), SDK.Title("Transparency Threshold")]                
        [SDK.Minimum(0),SDK.Default(0),SDK.Maximum( 255)]
        [SDK.InputValue("TransparencyThreshold")] public int TransparencyThreshold { get; set; }

        [SDK.Group(0), SDK.Title("Quantizer")]        
        [SDK.Default(Quantizer.Palette)]
        [SDK.InputValue("Quantizer")] public Quantizer Quantizer { get; set; }

        protected override EncoderAgent Evaluate()
        {
            var encoder = new GIFFORMAT.GifEncoder
            {
                IgnoreMetadata = this.IgnoreMetadata,                
                Quantizer = this.Quantizer.GetInstance()
            };

            return new EncoderAgent("GIF", encoder);
        }
    }    
}
