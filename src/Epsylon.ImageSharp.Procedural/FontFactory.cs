using System;
using System.Collections.Generic;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Text;

namespace Epsylon.ImageSharp.Procedural
{
    static class FontFactory
    {

        public static void TestGlyph()
        {
            var family = SixLabors.Fonts.SystemFonts.Find("arial");

            var font = new SixLabors.Fonts.Font(family, 50);

            var roptions = new SixLabors.Fonts.RendererOptions(font, 96);
            var size1 = SixLabors.Fonts.TextMeasurer.Measure("f", roptions);
            var size2 = SixLabors.Fonts.TextMeasurer.Measure("g", roptions);

        }

        public static Image<Rgba32> CreateGlyphImage(this SixLabors.Fonts.Font font, Char character, Rgba32 foreground)
        {
            var options = TextGraphicsOptions.Default;

            var text = character.ToString();

            var roptions = new SixLabors.Fonts.RendererOptions(font, 96);
            var size = SixLabors.Fonts.TextMeasurer.Measure(text, roptions);

            int w = (int)Math.Ceiling(size.Width);
            int h = (int)Math.Ceiling(size.Height);
            
            var img = new Image<Rgba32>(w,h);            

            img.Mutate(dc => dc.DrawText(options, text, font, foreground, System.Numerics.Vector2.Zero));

            return img;
        }

    }
}
