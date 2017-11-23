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
    using IMAGEENCODER = KeyValuePair<string, Action<UberFactory.SDK.ExportContext, IMAGE32>>;
    using IMAGE32DC = IImageProcessingContext<Rgba32>;

    [SDK.ContentNode("ImageReader")]
    [SDK.Title("File")]
    [SDK.TitleFormat( "{0} File")]
    public sealed class ImageReader : SDK.FileReader<IMAGE32>
    {
        public override string GetFileFilter()
        {
            var extensions = SixLabors.ImageSharp.Configuration.Default.ImageFormats.GetPickFileFilter();            

            return extensions + "|Vector Files|*.svg";
        }

        protected override IMAGE32 ReadFile(SDK.ImportContext stream)
        {
            var g = this.GetSharedSettings<GlobalSettings>();

            return g.ReadImage(stream);
        }

        protected override object EvaluatePreview(SDK.PreviewContext context) { return Evaluate().CreatePreview(context); }
    }

    [SDK.ContentNode("ImageWriter")]
    [SDK.Title("Save ImageSharp File (Advanced)")]
    public sealed class ImageWriter : SDK.FileWriter
    {
        [SDK.InputNode("Image")]
        public IMAGE32 Image { get; set; }

        [SDK.InputNode("Encoder")]
        [SDK.Title("Encoder")]
        public IMAGEENCODER Encoder { get; set; }        

        protected override string GetFileExtension() { return Encoder.Value == null ? "default" : Encoder.Key; }

        protected override void WriteFile(SDK.ExportContext stream)
        {
            if (Image == null) return;

            var g = this.GetSharedSettings<GlobalSettings>();

            g.WriteImage(stream, Image, Encoder.Value);            

            Image.Dispose();
        }        

        internal static IMAGEENCODER CreateEncoder(SixLabors.ImageSharp.Formats.IImageEncoder encoder, string ext)
        {
            Action<SDK.ExportContext, IMAGE32> act = (ctx,img) => ctx.WriteStream(s => img.Save(s, encoder));

            return encoder == null ? default(IMAGEENCODER) : new IMAGEENCODER(ext,act);
        }
    }


    public enum ImageWriterBasicFormat
    {
        PNG_Grayscale,
        PNG_Color,
        PNG_Grayscale_Alpha,
        PNG_Color_Alpha,
        PNG_Palette,
        JPG,
        GIF,
    }

    [SDK.ContentNode("ImageWriterBasic")]
    [SDK.Title("Save ImageSharp File (Basic)")]
    public sealed class ImageWriterBasic : SDK.FileWriter
    {
        [SDK.InputNode("Image")]
        public IMAGE32 Image { get; set; }

        [SDK.InputNode("Format")]
        [SDK.Title("Format")]
        public ImageWriterBasicFormat Format { get; set; }

        protected override string GetFileExtension()
        {
            /*
            if (Format == ImageWriterBasicFormat.GIF) return "gif";
            if (Format == ImageWriterBasicFormat.PNG_24) return "png";
            if (Format == ImageWriterBasicFormat.PNG_32) return "png";
            if (Format == ImageWriterBasicFormat.JPG) return "jpg";
            */

            throw new NotSupportedException();
        }

        protected override void WriteFile(SDK.ExportContext stream)
        {
            if (Image == null) return;

            var g = this.GetSharedSettings<GlobalSettings>();

            IMAGEENCODER encoder = default(IMAGEENCODER);


            // if (Format == ImageWriterBasicFormat.PNG_24) encoder = ImageWriter.CreateEncoder(new SixLabors.ImageSharp.Formats.Png.PngEncoder() { }, "png");


            g.WriteImage(stream, Image, encoder.Value);

            Image.Dispose();
        }        
    }

    [SDK.ContentNode("BatchProcessor")]
    [SDK.Title("Process ImageSharp Batch")]
    public sealed class BatchProcessor : SDK.BatchProcessor<IMAGE32,IMAGE32>
    {
        [SDK.InputNode("Transforms", true)]
        [SDK.Title("Transforms")]
        [SDK.ItemsPanel("VerticalList")]
        public IMGTRANSFORM[] Transforms { get; set; }

        [SDK.InputNode("Encoder")]
        [SDK.Title("Encoder")]
        public IMAGEENCODER Encoder { get; set; }

        protected override IEnumerable<string> GetFileInExtensions()
        {
            return SixLabors.ImageSharp.Configuration.Default.ImageFormats
                .SelectMany(item => item.FileExtensions)
                .ToArray();
        }

        protected override string GetFileOutExtension() { return Encoder.Key; }        

        protected override IMAGE32 ReadFile(SDK.ImportContext stream)
        {
            var g = this.GetSharedSettings<GlobalSettings>();

            return g.ReadImage(stream);
        }

        protected override IMAGE32 Transform(IMAGE32 value)
        {
            // transform image
            value.Mutate(dc => _ProcessStack(dc));

            return value;
        }

        private void _ProcessStack(IMAGE32DC dc)
        {
            foreach (var xform in Transforms)
            {
                xform?.Invoke(dc);
            }
        }

        protected override void WriteFile(SDK.ExportContext stream, IMAGE32 value)
        {
            var encoder = this.Encoder;
            if (encoder.Value == null) throw new ArgumentNullException();

            // write image
            encoder.Value?.Invoke(stream, value);

            // dispose image
            value.Dispose();
        }        
    }
}
