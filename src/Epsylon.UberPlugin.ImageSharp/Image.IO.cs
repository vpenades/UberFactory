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

    public abstract class ImageFilter : SDK.ContentFilter<IMAGE32>
    {
        protected override object EvaluatePreview(SDK.PreviewContext context)
        {
            return Evaluate().CreatePreview(context);
        }
    }
    

    [SDK.ContentNode("ImageReader")]    
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
    [SDK.Title("Save ImageSharp to File")]
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
            void act(SDK.ExportContext ctx, IMAGE32 img) => ctx.WriteStream(s => img.Save(s, encoder));

            return encoder == null ? default(IMAGEENCODER) : new IMAGEENCODER(ext,act);
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

        protected override void WriteFile(SDK.ExportContext stream, IMAGE32 image)
        {
            var encoder = this.Encoder;
            if (encoder.Value == null) throw new ArgumentNullException();

            // write image
            encoder.Value?.Invoke(stream, image);

            // dispose image
            image.Dispose();
        }        
    }
}
