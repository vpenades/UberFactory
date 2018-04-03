using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Overlays;
using SixLabors.ImageSharp.Processing.Drawing;
using SixLabors.ImageSharp.Processing.Text;

using Epsylon.ImageSharp.Procedural;

namespace Epsylon.UberPlugin
{
    using UberFactory;

    using PIXEL32 = Rgba32;
    using IMAGE32 = Image<Rgba32>;
    using SixLabors.Primitives;

    [SDK.Icon(Constants.ICON_TEXT), SDK.Title("System Font")]
    [SDK.ContentNode("SixLaborsSystemFont")]
    public sealed class SixLaborsSystemFont : SDK.ContentFilter<SixLabors.Fonts.FontFamily>
    {
        [SDK.InputValue("FontFamily")]
        [SDK.ViewStyle("ComboBox")]
        [SDK.Default("Arial")]
        [SDK.MetaDataEvaluate("Values", nameof(AvailableFontFamilies))]
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

            using (var o = ffamily.RenderText("Lorem ipsum dolor sit amet", 48, 4, PIXEL32.Black, TextGraphicsOptions.Default) as IMAGE32)
            {
                return o.CreatePreview(context);
            }
        }
    }

    [SDK.Icon(Constants.ICON_TEXT), SDK.Title("Text"), SDK.TitleFormat("{0} Text")]
    [SDK.ContentNode("CreateText")]
    public sealed class ImageSharpCreateText : ImageFilter
    {
        [SDK.InputNode("Text")]
        public String Text { get; set; }

        [SDK.InputNode("FontFamily")]
        [SDK.Default(typeof(SixLaborsSystemFont))]
        public SixLabors.Fonts.FontFamily FontFamily { get; set; }

        [SDK.Group(2)]
        [SDK.InputValue("Size")]
        [SDK.Minimum(1), SDK.Default(12), SDK.Maximum(1000)]
        public float Size { get; set; }

        [SDK.Group(2)]
        [SDK.InputValue("Padding")]
        [SDK.Minimum(0), SDK.Default(1), SDK.Maximum(1000)]
        public float Padding { get; set; }

        [SDK.Group(2)]
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



    [SDK.Icon(Constants.ICON_TEXT), SDK.Title("Font Grid"), SDK.TitleFormat("{0} Font Grid")]
    [SDK.ContentNode("CreateFontGrid")]
    public sealed class ImageSharpCreateFontGrid : ImageFilter
    {
        [SDK.InputNode("FontFamily")]
        [SDK.Default(typeof(SixLaborsSystemFont))]
        public SixLabors.Fonts.FontFamily FontFamily { get; set; }

        [SDK.Group(2)]
        [SDK.InputValue("Size")]
        [SDK.Minimum(1), SDK.Default(12), SDK.Maximum(1000)]
        public float Size { get; set; }

        [SDK.Group(2)]
        [SDK.InputValue("Padding")]
        [SDK.Minimum(0), SDK.Default(1), SDK.Maximum(1000)]
        public int Padding { get; set; }

        [SDK.Group(2)]
        [SDK.InputValue("Color")]
        [SDK.Default((UInt32)0xff000000)]
        [SDK.ViewStyle("ColorPicker")]
        public UInt32 Color { get; set; }

        protected override IMAGE32 Evaluate()
        {
            if (FontFamily == null) return null;
            if (Size <= 0) return null;

            var font = new SixLabors.Fonts.Font(FontFamily, Size);
            var context = new SixLabors.Fonts.RendererOptions(font);

            var characters = "0123456789ABCDEFGHIJKLM";

            var padding = new Size(this.Padding, this.Padding);

            var cellSize = context.GetGlypthMaxBounds(characters.ToCharArray()).OuterRound().Size;            

            cellSize += padding * 2;

            var image = ImageFactory.CreateImage<Rgba32>(cellSize * 16);

            foreach(var c in characters.ToArray())
            {
                _DrawGlyph(image, cellSize, padding, context, c);
            }

            return image;
        }

        private static void _DrawGlyph(Image<Rgba32> dstImage, Size cellSize, Size padding,  SixLabors.Fonts.RendererOptions context, char c)
        {
            var x = ((int)c) % 16;
            var y = ((int)c) / 16;

            x *= cellSize.Width;
            y *= cellSize.Height;

            x += padding.Width;
            y += padding.Height;

            var glyph = context.GetCharacterGlypth(c);
            var bounds = glyph.Bounds;

            dstImage.DrawGlypth(context, x, y, c);
        }
    }

}
