using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;

using PIXEL32 = SixLabors.ImageSharp.Rgba32;
using IMAGE32 = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.Rgba32>;

namespace Epsylon.UberPlugin
{
    using UberFactory;    

    public enum NoiseTypes { Perlin, ImprovedPerlin }

    [SDK.ContentNode("CreateNoise")]
    [SDK.Title("Noise"),SDK.TitleFormat( "{0} Noise")]
    public sealed class ImageSharpCreateNoise : ImageFilter
    {
        [SDK.InputValue("Width")]        
        [SDK.Title("W"), SDK.Group("Size")]
        [SDK.Default(256)]
        public int Width { get; set; }

        [SDK.InputValue("Height")]        
        [SDK.Title("H"), SDK.Group("Size")]
        [SDK.Default(256)]
        public int Height { get; set; }

        [SDK.InputValue("NoiseType")]        
        [SDK.Title("Type"),SDK.Group("Noise")]
        public NoiseTypes NoiseType { get; set; }

        [SDK.InputValue("RandomSeed")]        
        [SDK.Title("Seed"), SDK.Group("Noise")]
        [SDK.Minimum(0),SDK.Default(177),SDK.Maximum( 255)]
        public int RandomSeed { get; set; }

        [SDK.InputValue("Scale")]
        [SDK.Title("Scale"), SDK.Group("Noise")]
        [SDK.Minimum(2),SDK.Default(16)]
        public float Scale { get; set; }

        [SDK.InputValue("Color1")]
        [SDK.Title("Color A"), SDK.Group("Tint")]
        [SDK.Default((UInt32)0xff000000)]
        [SDK.ViewStyle("ColorPicker")]
        public UInt32 Color1 { get; set; }

        [SDK.InputValue("Color2")]
        [SDK.Title("Color A"), SDK.Group("Tint")]
        [SDK.Default((UInt32)0xffffffff)]
        [SDK.ViewStyle("ColorPicker")]
        public UInt32 Color2 { get; set; }

        protected override IMAGE32 Evaluate()
        {
            INoiseGenerator noiseGen = null;

            if (NoiseType == NoiseTypes.Perlin) noiseGen = new PerlinNoise3(256, RandomSeed);
            if (NoiseType == NoiseTypes.ImprovedPerlin) noiseGen = new ImprovedPerlinNoise(RandomSeed);

            var image = _ImageSharpExtensions.RenderNoise(Width, Height, noiseGen, Scale);

            var c1 = new Rgba32(Color1).ToVector4();
            var c2 = new Rgba32(Color2).ToVector4();

            image.MutatePixels
                (
                    pixel =>
                    {
                        var z = ((float)pixel.R) / 255.0f;

                        var c = System.Numerics.Vector4.Lerp(c1, c2, z);

                        return new Rgba32(c);
                    }
                );

            return image;
        }
    }

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

            var options = SixLabors.ImageSharp.Drawing.TextGraphicsOptions.Default;

            var txt = Text;
            if (txt == null) txt = String.Empty;

            return FontFamily.RenderText(txt, Size, Padding, new PIXEL32(Color), options);
        }        
    }

    [SDK.ContentNode("SixLaborsSystemFont")]
    [SDK.Title("System Font")]
    public sealed class SixLaborsSystemFont : SDK.ContentFilter<SixLabors.Fonts.FontFamily>
    {
        [SDK.InputValue("FontFamily")]
        [SDK.ViewStyle("ComboBox")]
        [SDK.Default("Arial")]
        [SDK.MetaDataEvaluate("Values",nameof(AvailableFontFamilies))]
        public String FontFamily { get; set; }

        public String[] AvailableFontFamilies => new SixLabors.Fonts.FontCollection().Families.Select(item => item.Name).ToArray();

        protected override SixLabors.Fonts.FontFamily Evaluate()
        {
            return new SixLabors.Fonts.FontCollection().Find(FontFamily);
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
    

}
