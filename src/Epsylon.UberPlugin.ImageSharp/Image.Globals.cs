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
    
    using IMGTRANSFORM = Action<IImageProcessingContext<Rgba32>>;
    using IMAGEENCODER = Action<UberFactory.SDK.ExportContext, IMAGE32>;    

    [SDK.ContentNode("GlobalSettings")]
    [SDK.Title("ImageSharp Settings")]
    public class GlobalSettings : SDK.ContentObject
    {
        [SDK.InputNode("PreFormatting", true)]
        [SDK.Title("Pre Processing"), SDK.Group("Transforms"), SDK.ItemsPanel("VerticalList")]
        public IMGTRANSFORM[] PreProcessing { get; set; }

        [SDK.InputNode("PostFormatting", true)]
        [SDK.Title("Post Processing"), SDK.Group("Transforms"), SDK.ItemsPanel("VerticalList")]
        public IMGTRANSFORM[] PostProcessing { get; set; }

        public IMAGE32 ReadImage(SDK.ImportContext stream)
        {
            var image = stream.ReadStream(s => Image.Load(s));

            if (image == null) return null;

            if (PreProcessing != null)
            {
                foreach (var xform in PreProcessing)
                {
                    image.Mutate(dc => xform(dc));
                }
            }

            return image;
        }    

        public void WriteImage(SDK.ExportContext stream, IMAGE32 image, IMAGEENCODER encoder)
        {
            if (image == null) return;

            if (stream == null) throw new ArgumentNullException(nameof(stream));            
            if (encoder== null) throw new ArgumentNullException(nameof(encoder));

            if (PostProcessing != null)
            {
                foreach(var xform in PostProcessing)
                {
                    image.Mutate(dc => xform(dc));
                }
            }

            encoder?.Invoke(stream, image);
        }
    }

    [SDK.ContentNode("JpegGlobalSettings")]
    [SDK.Title("ImageSharp JPEG Settings")]
    public class JpegGlobalSettings : SDK.ContentObject
    {
        [SDK.InputValue("Quality")]
        [SDK.Minimum(0), SDK.Default(80), SDK.Maximum( 100)]
        [SDK.ViewStyle("Slider")]
        public int Quality { get; set; }

        public IMAGEENCODER GetEncoder()
        {
            var encoder = new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
            {
                Quality = Quality
            };

            void act(SDK.ExportContext ctx, IMAGE32 img) => ctx.WriteStream(s => img.Save(s, encoder));

            return act;
        }
    }

    [SDK.ContentNode("PngGlobalSettings")]
    [SDK.Title("ImageSharp PNG Settings")]
    public class PngGlobalSettings : SDK.ContentObject
    {
        [SDK.InputValue("CompressionLevel")]
        [SDK.Minimum(1), SDK.Default(6), SDK.Maximum(9)]
        [SDK.Title("Compression Level")]
        [SDK.ViewStyle("Slider")]
        public int CompressionLevel { get; set; }

        public IMAGEENCODER GetEncoder(SixLabors.ImageSharp.Formats.Png.PngColorType colorType)
        {
            var encoder = new SixLabors.ImageSharp.Formats.Png.PngEncoder
            {
                CompressionLevel = this.CompressionLevel,
                PngColorType = colorType,
            };

            void act(SDK.ExportContext ctx, IMAGE32 img) => ctx.WriteStream(s => img.Save(s, encoder));

            return act;
        }
    }
}
