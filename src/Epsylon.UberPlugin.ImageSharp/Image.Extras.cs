using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;

using Epsylon.ImageSharp.Procedural;

using PIXEL32 = SixLabors.ImageSharp.Rgba32;
using IMAGE32 = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.Rgba32>;

namespace Epsylon.UberPlugin
{
    using SixLabors.ImageSharp.PixelFormats;
    using UberFactory;

    [SDK.Icon("🏴")]
    [SDK.ContentNode("CreateSolidColor")]
    [SDK.Title("Solid Color"), SDK.TitleFormat("{0} Solid Color")]
    public sealed class ImageSharpCreateSolidColor : ImageFilter
    {
        [SDK.InputValue("Width")]
        [SDK.Title("W"), SDK.Group("Size")]
        [SDK.Minimum(1),SDK.Default(256)]
        public int Width { get; set; }

        [SDK.InputValue("Height")]
        [SDK.Title("H"), SDK.Group("Size")]
        [SDK.Minimum(1), SDK.Default(256)]
        public int Height { get; set; }

        [SDK.InputValue("Color")]
        [SDK.Default((UInt32)0xff000000)]
        [SDK.ViewStyle("ColorPicker")]
        public UInt32 Color { get; set; }

        protected override IMAGE32 Evaluate()
        {
            var img = new IMAGE32(this.Width, this.Height);

            img.Mutate(dc => dc.BackgroundColor(new PIXEL32(Color)));

            return img;
        }
    }    
    
    [SDK.Icon("🏁")]
    [SDK.ContentNode("CreateCheckers")]
    [SDK.Title("Checkers"), SDK.TitleFormat("{0} Checkers")]
    public sealed class ImageSharpCreateCheckers : ImageFilter
    {
        [SDK.InputValue("Width")]
        [SDK.Title("W"), SDK.Group("Size")]
        [SDK.Minimum(1), SDK.Default(256)]
        public int Width { get; set; }

        [SDK.InputValue("Height")]
        [SDK.Title("H"), SDK.Group("Size")]
        [SDK.Minimum(1), SDK.Default(256)]
        public int Height { get; set; }

        [SDK.InputValue("CellWidth")]
        [SDK.Title("W"), SDK.Group("Cell Size")]
        [SDK.Minimum(1), SDK.Default(16)]
        public int CellWidth { get; set; }

        [SDK.InputValue("CellHeight")]
        [SDK.Title("H"), SDK.Group("Cell Size")]
        [SDK.Minimum(1), SDK.Default(16)]
        public int CellHeight { get; set; }

        [SDK.InputValue("OddColor")]
        [SDK.Default((UInt32)0xffffffff)]
        [SDK.ViewStyle("ColorPicker")]
        public UInt32 OddColor { get; set; }

        [SDK.InputValue("EvenColor")]
        [SDK.Default((UInt32)0xff000000)]
        [SDK.ViewStyle("ColorPicker")]
        public UInt32 EvenColor { get; set; }

        protected override IMAGE32 Evaluate()
        {
            var img = new IMAGE32(this.Width, this.Height);

            img.Mutate(dc => dc.FillCheckers(this.CellWidth,this.CellHeight, new PIXEL32(OddColor) , new PIXEL32(EvenColor)) );

            return img;
        }
    }

    [SDK.Icon("🏳️‍🌈")]    
    [SDK.ContentNode("CreatePerlinNoise")]
    [SDK.Title("Noise"),SDK.TitleFormat( "{0} Noise")]
    public sealed class ImageSharpCreatePerlinNoise : ImageFilter
    {
        [SDK.InputValue("Width")]        
        [SDK.Title("W"), SDK.Group("Size")]
        [SDK.Minimum(1), SDK.Default(256)]
        public int Width { get; set; }

        [SDK.InputValue("Height")]        
        [SDK.Title("H"), SDK.Group("Size")]
        [SDK.Minimum(1), SDK.Default(256)]
        public int Height { get; set; }        

        [SDK.InputValue("RandomSeed")]        
        [SDK.Title("Seed"), SDK.Group("Noise")]
        [SDK.Minimum(0),SDK.Default(177),SDK.Maximum( 255)]
        public int RandomSeed { get; set; }

        [SDK.InputValue("Scale")]
        [SDK.Title("Scale"), SDK.Group("Noise")]
        [SDK.Minimum(2),SDK.Default(16)]
        public float Scale { get; set; }

        [SDK.InputValue("Octaves")]
        [SDK.Title("Octaves"), SDK.Group("Noise")]
        [SDK.Minimum(1), SDK.Default(8)]
        public int Octaves { get; set; }

        [SDK.InputValue("Persistence")]
        [SDK.Title("Persistence"), SDK.Group("Noise")]
        [SDK.Minimum(0), SDK.Default(50),SDK.Maximum(100)]
        [SDK.ViewStyle("Slider")]
        public int Persistence { get; set; }

        [SDK.InputNode("Gradient")]
        [SDK.Title("Gradient"), SDK.Group("Tint")]        
        public PIXEL32[] Gradient { get; set; }

        protected override IMAGE32 Evaluate()
        {
            var p = (float)Persistence;

            using (var noise = new Image<HalfSingle>(Width,Height))
            {
                noise.Mutate(dc => dc.FillPerlinNoise(this.Scale, 0, this.Octaves, p / 100.0f, this.RandomSeed));                
                
                return noise.CloneWithLookupTable(Gradient);
            }
        }
    }

    [SDK.Icon("AZ")]
    [SDK.ContentNode("CreateText")]
    [SDK.Title("Text"),SDK.TitleFormat( "{0} Text")]
    public sealed class ImageSharpCreateText : ImageFilter
    {
        [SDK.InputNode("Text")]
        public String Text { get; set; }

        [SDK.InputNode("FontFamily")]
        [SDK.Default(typeof(SixLaborsSystemFont))]
        public SixLabors.Fonts.FontFamily FontFamily { get; set; }

        [SDK.InputValue("Size")]
        [SDK.Minimum(1),SDK.Default(12),SDK.Maximum(1000)]
        public float Size { get; set; }

        [SDK.InputValue("Padding")]
        [SDK.Minimum(0),SDK.Default(1),SDK.Maximum( 1000)]
        public float Padding { get; set; }

        [SDK.InputValue("Color")]        
        [SDK.Default((UInt32)0xff000000)]
        [SDK.ViewStyle("ColorPicker")]
        public UInt32 Color { get; set; }

        protected override IMAGE32 Evaluate()
        {
            if (FontFamily == null) return null;

            var options = TextGraphicsOptions.Default;

            var txt = Text;
            if (txt == null) txt = String.Empty;

            return FontFamily.RenderText(txt, Size, Padding, new PIXEL32(Color), options);
        }        
    }

    [SDK.Icon("AZ")]
    [SDK.ContentNode("SixLaborsSystemFont")]
    [SDK.Title("System Font")]
    public sealed class SixLaborsSystemFont : SDK.ContentFilter<SixLabors.Fonts.FontFamily>
    {
        [SDK.InputValue("FontFamily")]
        [SDK.ViewStyle("ComboBox")]
        [SDK.Default("Arial")]
        [SDK.MetaDataEvaluate("Values",nameof(AvailableFontFamilies))]
        public String FontFamily { get; set; }

        public String[] AvailableFontFamilies => SixLabors.Fonts.SystemFonts.Families.Select(item => item.Name).ToArray();

        protected override SixLabors.Fonts.FontFamily Evaluate()
        {
            return SixLabors.Fonts.SystemFonts.TryFind(FontFamily, out SixLabors.Fonts.FontFamily font)
                ?
                font
                :
                SixLabors.Fonts.SystemFonts.Find("Arial");
        }

        protected override object EvaluatePreview(SDK.PreviewContext context)
        {
            var ffamily = Evaluate(); if (ffamily == null) return null;

            using (var o = ffamily.RenderText("Lorem ipsum dolor sit amet", 48, 4, PIXEL32.Black, SixLabors.ImageSharp.Drawing.TextGraphicsOptions.Default) as IMAGE32)
            {
                return o.CreatePreview(context);
            }
        }
    }

    [SDK.Icon("ℹ")]
    [SDK.ContentNode("ImageMetadataToText")]
    [SDK.Title("Image Metadata"), SDK.TitleFormat("{0} Metadata")]
    public sealed class ImageSharpMetadataToText : SDK.ContentFilter<String>
    {
        [SDK.InputNode("Image")]
        public IMAGE32 Image { get; set; }

        protected override string Evaluate()
        {
            if (Image == null) return null;
            if (Image.MetaData == null) return null;

            var sb = new StringBuilder();

            var exif = Image.MetaData.ExifProfile;
            var icc = Image.MetaData.IccProfile;
            var ipps = Image.MetaData.Properties;

            if (exif != null)
            {
                foreach (var exifval in exif.Values)
                {
                    sb.AppendLine($"{exifval.Tag} = {exifval.Value}");
                }
            }

            if (icc != null)
            {
                foreach (var iccval in icc.Entries)
                {
                    sb.AppendLine($"{iccval.Signature} {iccval.TagSignature}");
                }
            }

            if (ipps != null)
            {
                foreach (var iprop in ipps)
                {
                    sb.AppendLine($"{iprop.Name} = {iprop.Value}");
                }
            }            

            return sb.ToString();
        }
    }



}
