using System;
using System.Collections.Generic;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;

namespace Epsylon.ImageSharp.Procedural
{
    
    public class CommonEffect<TPixel> where TPixel : struct, IPixel<TPixel>
    {
        // to perform the effects we need:
        // to keep the original image "as is"
        // to compose over a target image sequentially

        private readonly ShadowEffect<TPixel> DropShadow = new ShadowEffect<TPixel>();
        private readonly ShadowEffect<TPixel> InnerShadow = new ShadowEffect<TPixel>();

        private readonly InnerGlowEffect<TPixel> InnerGlow = new InnerGlowEffect<TPixel>();
        private readonly OuterGlowEffect<TPixel> OuterGlow = new OuterGlowEffect<TPixel>();

        private readonly BevelEffect<TPixel> Bevel = new BevelEffect<TPixel>();

        // Satin
        // texture
        // outline

        public void Mutate(Image<TPixel> image)
        {
            using (var source = image.Clone())
            {
                image.Mutate(dc => dc.Fill(default(TPixel)));

                DropShadow.ComposeLayer(image, source);
                OuterGlow.ComposeLayer(image, source);
                image.Mutate(dc => dc.DrawImage(source, 1, new Size(source.Width, source.Height), Point.Empty));
                InnerShadow.ComposeLayer(image, source);
                InnerGlow.ComposeLayer(image, source);
                // Bevel;
            }
        }
        
    }


    abstract class CommonLayerEffect<TPixel> where TPixel : struct, IPixel<TPixel>
    {
        public bool Enabled { get; set; }

        public abstract void ComposeLayer(Image<TPixel> target, Image<TPixel> source);

        protected static Point GetOrigin(int angle, int dist)
        {
            if (dist == 0) return Point.Empty;

            var a = (double)angle / (Math.PI * 2);
            var x = Math.Cos(a) * dist;
            var y = -Math.Sign(a) * dist;

            return new Point((int)x, (int)y);
        }

        protected static void ComposeBlurLayer(Image<TPixel> target, Image<TPixel> source, PixelBlenderMode blend, UInt32 tint, int radius, int opacity, bool maskAlpha = false, bool invertAlpha = false, int angle=0, int distance=0)
        {
            var size = new Size(source.Width, source.Height);
            var origin = GetOrigin(angle, distance);

            var color = default(TPixel); color.PackFromRgba32(new Rgba32(tint));

            using (var layer = source.Clone())
            {
                layer.Mutate(dc => dc
                    .FillRGB(color)
                    .GaussianBlur(radius)
                );

                _SetupAlpha(layer, source, maskAlpha,invertAlpha);

                target.Mutate(dc => dc.DrawImage(layer, blend, (float)opacity / 100.0f, size, origin));
            }
        }

        private static void _SetupAlpha(Image<TPixel> layer, Image<TPixel> source, bool maskAlpha, bool invertAlpha)
        {
            if (!maskAlpha && !invertAlpha) return;

            for(int y=0; y < layer.Height; ++y)
            {
                for (int x = 0; x < layer.Width; ++x)
                {
                    var c = layer[x, y].ToVector4();
                    if (invertAlpha) c.W = 1 - c.W;
                    if (maskAlpha) c.W *= source[x, y].ToVector4().W;
                }
            }

        }
    }

    // https://www.adobe.com/devnet-apps/photoshop/fileformatashtml/#50577409_22203
    // for Drop and Inner shadow
    class ShadowEffect<TPixel> : CommonLayerEffect<TPixel> where TPixel : struct, IPixel<TPixel>
    {
        // in pixels
        public int BlurRadius { get; set; }

        // %
        public int Intensity { get; set; }

        // degrees
        public int Angle { get; set; }

        // in pixels
        public int Distance { get; set; }

        public UInt32 Color { get; set; }

        public PixelBlenderMode BlendMode { get; set; }

        // %
        public int Opacity { get; set; }

        public override void ComposeLayer(Image<TPixel> target, Image<TPixel> source)
        {
            ComposeBlurLayer(target, source, BlendMode, Color, BlurRadius, Opacity, false,false, Angle, Distance);
        }
    }

    // https://www.adobe.com/devnet-apps/photoshop/fileformatashtml/#50577409_25738
    class OuterGlowEffect<TPixel> : CommonLayerEffect<TPixel> where TPixel : struct, IPixel<TPixel>
    {
        // in pixels
        public int BlurRadius { get; set; }

        // %
        public int Intensity { get; set; }

        public UInt32 Color { get; set; }

        public PixelBlenderMode BlendMode { get; set; }

        // %
        public int Opacity { get; set; }

        public override void ComposeLayer(Image<TPixel> target, Image<TPixel> source)
        {
            ComposeBlurLayer(target, source, BlendMode, Color, BlurRadius, Opacity);
        }
    }

    // https://www.adobe.com/devnet-apps/photoshop/fileformatashtml/#50577409_27692
    class InnerGlowEffect<TPixel> : CommonLayerEffect<TPixel> where TPixel : struct, IPixel<TPixel>
    {
        // in pixels
        public int BlurRadius { get; set; }

        // %
        public int Intensity { get; set; }

        public UInt32 Color { get; set; }

        public PixelBlenderMode BlendMode { get; set; }

        // %
        public int Opacity { get; set; }

        public bool Invert { get; set; }

        public override void ComposeLayer(Image<TPixel> target, Image<TPixel> source)
        {
            ComposeBlurLayer(target, source, BlendMode, Color, BlurRadius, Opacity, true, Invert);
        }
    }

    // https://www.adobe.com/devnet-apps/photoshop/fileformatashtml/PhotoshopFileFormats.htm#50577409_31889
    class BevelEffect<TPixel> : CommonLayerEffect<TPixel> where TPixel : struct, IPixel<TPixel>
    {
        // degrees
        public int Angle { get; set; }

        // in pixels
        public int Strength { get; set; }

        // in pixels
        public int BlurRadius { get; set; }

        public PixelBlenderMode HighlightBlendMode { get; set; }

        public PixelBlenderMode ShadowBlendMode { get; set; }

        public int BevelStyle { get; set; } // this should be an enum

        // %
        public int HighlightOpacity { get; set; }

        // %
        public int ShadowOpacity { get; set; }

        public bool UpOrDown { get; set; }

        public override void ComposeLayer(Image<TPixel> target, Image<TPixel> source)
        {
            ComposeBlurLayer(target, source, ShadowBlendMode,    0xff000000, BlurRadius, ShadowOpacity,    true, true, Angle, -Strength);
            ComposeBlurLayer(target, source, HighlightBlendMode, 0xffffffff, BlurRadius, HighlightOpacity, true, true, Angle, +Strength);
        }
    }


    
}
