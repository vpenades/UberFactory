﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Epsylon.UberPlugin.ImageSharp
{
    using UberFactory;

    using COLOR = Rgba32;    
    

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
            using (var image = stream.ReadStream(s => Image.Load(s)) )
            {
                var palette = new COLOR[image.Width];

                for (int x = 0; x < image.Width; ++x)
                {
                    palette[x] = image[x, image.Height / 2];
                }

                return palette;
            }
        }

        protected override object EvaluatePreview(SDK.PreviewContext context)
        {
            // TODO: create image with palette
            return null; 
        }
    }

    [SDK.Icon(Constants.ICON_COLOR)]
    [SDK.ContentNode("PaletteFromTwoColors")]
    [SDK.Title("Palette with 2 colors")]
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

    [SDK.Icon(Constants.ICON_COLOR)]
    [SDK.ContentNode("ColorValue")]
    [SDK.Title("Color")]
    [SDK.TitleFormat("{0} Color")]
    public sealed class ColorValue : SDK.ContentFilter<COLOR>
    {
        [SDK.InputValue("Color")]
        [SDK.Title("Color")]
        [SDK.Default((UInt32)0xffffffff)]
        [SDK.ViewStyle("ColorPicker")]
        public UInt32 Color { get; set; }
        
        protected override COLOR Evaluate()
        {
            return new COLOR(this.Color);
        }
    }

    [SDK.Icon(Constants.ICON_COLOR)]
    [SDK.ContentNode("PaletteFromColors")]
    [SDK.Title("Palette")]
    [SDK.TitleFormat("{0} Palette")]
    public sealed class PaletteFromColors : SDK.ContentFilter<COLOR[]>
    {
        [SDK.InputNode("Colors",true)]
        [SDK.Title("Colors")]        
        public COLOR[] Colors { get; set; }        

        protected override COLOR[] Evaluate()
        {
            return Colors;
        }
    }



}
