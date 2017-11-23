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
    [SDK.ContentMetaData("Title", "ImageSharp Settings")]
    public class GlobalSettings : SDK.ContentObject
    {
        [SDK.InputNode("PreFormatting", true)]
        [SDK.InputMetaData("Title", "Pre Processing")]
        [SDK.InputMetaData("Panel", "VerticalList")]
        public IMGTRANSFORM[] PreProcessing { get; set; }

        [SDK.InputNode("PostFormatting", true)]
        [SDK.InputMetaData("Title", "Post Processing")]
        [SDK.InputMetaData("Panel", "VerticalList")]
        public IMGTRANSFORM[] PostProcessing { get; set; }

        public IMAGE32 ReadImage(SDK.ImportContext stream)
        {            
            if (stream.FileName.EndsWith(".svg"))
            {
                var svg = new SkiaSharp.Extended.Svg.SKSvg(96);
                stream.ReadStream(s => svg.Load(s));

                using (var bmp = svg.Render())
                {
                    return bmp.ToImageSharp();
                }
            }

            // HL.IconPro.Lib.Core.IconFormat.Default.Configure();

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
    [SDK.ContentMetaData("Title", "ImageSharp JPEG Settings")]
    public class JpegGlobalSettings : SDK.ContentObject
    {
        [SDK.InputValue("Quality")]
        [SDK.InputMetaData("Minimum", 0)]
        [SDK.InputMetaData("Default", 80)]
        [SDK.InputMetaData("Maximum", 100)]
        [SDK.InputMetaData("ViewStyle", "Slider")]
        public int Quality { get; set; }
    }
}
