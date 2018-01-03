using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberPlugin.ImageSharp
{
    using COLOR = SixLabors.ImageSharp.Rgba32;

    using UberFactory;
    using SixLabors.ImageSharp;

    [SDK.ContentNode("PaletteReader")]
    [SDK.Title("Palette from File")]
    [SDK.TitleFormat("{0} File")]
    public sealed class PaletteReader : SDK.FileReader<COLOR[]>
    {
        public override string GetFileFilter()
        {
            return SixLabors.ImageSharp.Configuration.Default.ImageFormats.GetPickFileFilter();            
        }

        protected override COLOR[] ReadFile(SDK.ImportContext stream)
        {
            var g = this.GetSharedSettings<GlobalSettings>();

            var image = g.ReadImage(stream);

            var palette = new COLOR[image.Width];

            for(int x=0; x < image.Width; ++x)
            {
                palette[x] = image[x, image.Height / 2];
            }

            return palette;
        }

        protected override object EvaluatePreview(SDK.PreviewContext context)
        {
            // TODO: create image with palette
            return null; 
        }
    }


    [SDK.ContentNode("PaletteFromTwoColors")]
    [SDK.Title("2 colors Palette")]
    [SDK.TitleFormat("{0} Palette")]
    public sealed class PaletteFromTwoColors : SDK.ContentFilter<COLOR[]>
    {
        [SDK.InputValue("Color1")]
        [SDK.Title("Color A"), SDK.Group("Tint")]
        [SDK.Default((UInt32)0xff000000)]
        [SDK.ViewStyle("ColorPicker")]
        public UInt32 Color1 { get; set; }

        [SDK.InputValue("Color2")]
        [SDK.Title("Color B"), SDK.Group("Tint")]
        [SDK.Default((UInt32)0xffffffff)]
        [SDK.ViewStyle("ColorPicker")]
        public UInt32 Color2 { get; set; }

        protected override COLOR[] Evaluate()
        {
            return new COLOR[] { new COLOR(Color1), new COLOR(Color2) };
        }
    }
}
