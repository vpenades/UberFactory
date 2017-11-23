using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SixLabors.ImageSharp;

using PIXEL32 = SixLabors.ImageSharp.Rgba32;
using IMAGE32 = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.Rgba32>;

namespace Epsylon.UberPlugin
{
    using UberFactory;    

    public enum NoiseTypes { Perlin, ImprovedPerlin }

    [SDK.ContentNode("CreateNoise")]
    [SDK.ContentMetaData("Title", "Noise")]
    [SDK.ContentMetaData("TitleFormat", "{0} Noise")]
    public sealed class ImageSharpCreateNoise : SDK.ContentFilter<IMAGE32>
    {
        [SDK.InputValue("Width")]
        [SDK.InputMetaData("Group", "Size")]
        [SDK.InputMetaData("Title", "W")]
        [SDK.InputMetaData("Default", 256)]
        public int Width { get; set; }

        [SDK.InputValue("Height")]
        [SDK.InputMetaData("Group", "Size")]
        [SDK.InputMetaData("Title", "H")]
        [SDK.InputMetaData("Default", 256)]
        public int Height { get; set; }

        [SDK.InputValue("NoiseType")]
        [SDK.InputMetaData("Group", "Noise")]
        [SDK.InputMetaData("Title", "Type")]
        public NoiseTypes NoiseType { get; set; }

        [SDK.InputValue("RandomSeed")]
        [SDK.InputMetaData("Group", "Noise")]
        [SDK.InputMetaData("Title", "Seed")]
        [SDK.InputMetaData("Minimum", 0)]
        [SDK.InputMetaData("Default", 177)]
        [SDK.InputMetaData("Maximum", 255)]
        public int RandomSeed { get; set; }

        [SDK.InputValue("Scale")]
        [SDK.InputMetaData("Title", "Scale")]
        [SDK.InputMetaData("Minimum", 2)]
        [SDK.InputMetaData("Default", 16)]
        public float Scale { get; set; }

        protected override IMAGE32 Evaluate()
        {
            INoiseGenerator noiseGen = null;

            if (NoiseType == NoiseTypes.Perlin) noiseGen = new PerlinNoise3(256, RandomSeed);
            if (NoiseType == NoiseTypes.ImprovedPerlin) noiseGen = new ImprovedPerlinNoise(RandomSeed);

            return _ImageSharpExtensions.RenderNoise(Width, Height, noiseGen, Scale);            
        }

        protected override object EvaluatePreview(SDK.PreviewContext context) { return Evaluate().CreatePreview(context); }

    }

    [SDK.ContentNode("CreateText")]
    [SDK.ContentMetaData("Title", "Text")]
    [SDK.ContentMetaData("TitleFormat", "{0} Text")]
    public sealed class ImageSharpCreateText : SDK.ContentFilter<IMAGE32>
    {
        [SDK.InputNode("Text")]
        public String Text { get; set; }

        [SDK.InputNode("FontFamily")]
        [SDK.InputMetaData("Default",typeof(SixLaborsSystemFont))]
        public SixLabors.Fonts.FontFamily FontFamily { get; set; }

        [SDK.InputValue("Size")]
        [SDK.InputMetaData("Minimum",1)]
        [SDK.InputMetaData("Default",12)]
        [SDK.InputMetaData("Maximum",1000)]
        public float Size { get; set; }

        [SDK.InputValue("Padding")]
        [SDK.InputMetaData("Minimum", 0)]
        [SDK.InputMetaData("Default", 1)]
        [SDK.InputMetaData("Maximum", 1000)]
        public float Padding { get; set; }

        [SDK.InputValue("Color")]        
        [SDK.InputMetaData("Default", (UInt32)0xff000000)]
        [SDK.InputMetaData("ViewStyle", "ColorPicker")]
        public UInt32 Color { get; set; }

        protected override IMAGE32 Evaluate()
        {
            if (FontFamily == null) return null;

            var options = SixLabors.ImageSharp.Drawing.TextGraphicsOptions.Default;

            var txt = Text;
            if (txt == null) txt = String.Empty;

            return FontFamily.RenderText(txt, Size, Padding, new PIXEL32(Color), options);
        }

        protected override object EvaluatePreview(SDK.PreviewContext context) { return Evaluate().CreatePreview(context); }
    }

    [SDK.ContentNode("SixLaborsSystemFont")]
    [SDK.ContentMetaData("Title", "System Font")]
    public sealed class SixLaborsSystemFont : SDK.ContentFilter<SixLabors.Fonts.FontFamily>
    {
        [SDK.InputValue("FontFamily")]
        [SDK.InputMetaData("ViewStyle", "ComboBox")]
        [SDK.InputMetaData("Default","Arial")]
        [SDK.InputMetaDataEvaluate("Values",nameof(AvailableFontFamilies))]
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
