using System;
using System.Collections.Generic;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Text;

using SixLabors.Primitives;
using SixLabors.Shapes;

namespace Epsylon.ImageSharp.Procedural
{

    // we need a single GlypthBitmap/SpriteBitmap object to represent an image that can be styled.
    // option 1 is to have a custom object with an image inside
    // option 2 is to add the glyph/sprite info as Image Metadata.


    public static class FontFactory
    {
        // https://docs.microsoft.com/es-es/typography/opentype/spec/ttch01

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

        public static RectangleF GetGlypthMaxBounds(this SixLabors.Fonts.RendererOptions options, params char[] characters)
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
            var origin = new PointF(x, y);

            // origin += new SizeF(0.5f, 0.5f);

            var path = options
                .GetCharacterGlypth(character)
                .Translate(origin);            

            target.Mutate(dc => dc.Fill(Rgba32.White, path) );
        }               
    }


    class SpriteInfo
    {
        // logical origin/center/rotation pivot of the sprite
        public PointF Origin { get; set; }

        // it can be circles, polygons, etc, each one with a collision ID
        // private readonly List<CollisionShape> _CollisionShapes = new List<CollisionShape>();

        // glyph info
    }



}
