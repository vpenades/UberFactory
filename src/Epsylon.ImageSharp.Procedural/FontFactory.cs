using System;
using System.Collections.Generic;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Drawing;
using SixLabors.ImageSharp.Processing.Text;
using SixLabors.Shapes;

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

        // https://github.com/SixLabors/Fonts/issues/57

        public static SixLabors.Primitives.RectangleF GetGlypthMaxBounds(this SixLabors.Fonts.RendererOptions options, params char[] characters)
        {
            var rect = new SixLabors.Primitives.RectangleF(float.PositiveInfinity, float.PositiveInfinity, float.NegativeInfinity, float.NegativeInfinity);

            foreach(var c in characters)
            {
                var r = options.GetCharacterGlypth(c).Bounds;

                rect = rect.IsInitialized() ? rect.UnionWith(r) : r;
            }

            return rect;            
        }

        public static IPathCollection GetCharacterGlypth(this SixLabors.Fonts.RendererOptions options, char character)
        {
            return TextBuilder.GenerateGlyphs(string.Empty + character, options);
        }

        public static void DrawGlypth(this Image<Rgba32> target, SixLabors.Fonts.RendererOptions options, int x, int y, char character)
        {
            var path = options
                .GetCharacterGlypth(character)
                .Translate(x,y);            

            target.Mutate(dc => dc.Fill(Rgba32.White, path) );
        }       

        



    }
}
