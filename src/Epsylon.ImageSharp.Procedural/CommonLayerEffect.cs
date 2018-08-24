using System;
using System.Collections.Generic;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace Epsylon.ImageSharp.Procedural
{
    // https://github.com/KDE/krita/tree/f352cc1d4367542ad55d61636927e90c56e92b7d/libs/image/layerstyles
    // http://registry.gimp.org/node/186
    // https://dsp.stackexchange.com/questions/530/bitmap-alpha-bevel-algorithm
    // http://www.rw-designer.com/bevel-effect-explained


    public class CommonEffect<TPixel> where TPixel : struct, IPixel<TPixel>
    {
        #region data

        private readonly ConvolutionEffect<TPixel> _DropShadow = new ConvolutionEffect<TPixel>();
        private readonly ConvolutionEffect<TPixel> _InnerShadow = new ConvolutionEffect<TPixel>();

        private readonly ConvolutionEffect<TPixel> _InnerGlow = new ConvolutionEffect<TPixel>();
        private readonly ConvolutionEffect<TPixel> _OuterGlow = new ConvolutionEffect<TPixel>();

        private readonly BevelEffect<TPixel> _Bevel = new BevelEffect<TPixel>();

        #endregion

        #region properties

        public ConvolutionEffect<TPixel> DropShadow => _DropShadow;
        public ConvolutionEffect<TPixel> InnerShadow => _InnerShadow;

        public ConvolutionEffect<TPixel> InnerGlow => _InnerGlow;
        public ConvolutionEffect<TPixel> OuterGlow => _OuterGlow;

        public BevelEffect<TPixel> Bevel => _Bevel;

        // Satin
        // texture
        // outline

        #endregion

        #region API

        public void Mutate(Image<TPixel> image)
        {
            using (var source = image.Clone())
            {
                _ApplyInnerEffects(image, source);

                source.Mutate(dc => dc.BlitImage(image));

                _ApplyOuterEffects(image, source);                

                image.Mutate(dc => dc.DrawImage(source, 1));
            }
        }

        private void _ApplyInnerEffects(Image<TPixel> current, Image<TPixel> source)
        {            
            _InnerShadow.ComposeLayer(current, source);
            _InnerGlow.ComposeLayer(current, source);

            // this ensures we preserve the original alpha
            _TransferAlpha(current, source);
        }

        private void _ApplyOuterEffects(Image<TPixel> current, Image<TPixel> source)
        {
            _DropShadow.ComposeLayer(current, source);
            _OuterGlow.ComposeLayer(current, source);            
        }

        private static void _TransferAlpha(Image<TPixel> target, Image<TPixel> source)
        {
            for (int y = 0; y < target.Height; ++y)
            {
                for (int x = 0; x < target.Width; ++x)
                {
                    var c = target[x, y].ToVector4();

                    c.W = source[x, y].ToVector4().W;

                    var cc = default(TPixel); cc.PackFromVector4(c);
                    target[x, y] = cc;                    
                }
            }
        }

        private static void _TransferColor(Image<TPixel> target, Image<TPixel> source)
        {
            for (int y = 0; y < target.Height; ++y)
            {
                for (int x = 0; x < target.Width; ++x)
                {
                    var sc = source[x, y].ToVector4();
                    var tc = target[x, y].ToVector4();

                    sc.W = tc.W;                    

                    var cc = default(TPixel); cc.PackFromVector4(sc);
                    target[x, y] = cc;
                }
            }
        }

        #endregion
    }


    public abstract class CommonLayerEffect<TPixel> where TPixel : struct, IPixel<TPixel>
    {
        public bool Enabled { get; set; }

        public abstract void ComposeLayer(Image<TPixel> target, Image<TPixel> source);        
    }

    // https://www.adobe.com/devnet-apps/photoshop/fileformatashtml/#50577409_22203
    // https://www.adobe.com/devnet-apps/photoshop/fileformatashtml/#50577409_25738
    // https://www.adobe.com/devnet-apps/photoshop/fileformatashtml/#50577409_27692
    public class ConvolutionEffect<TPixel> : CommonLayerEffect<TPixel> where TPixel : struct, IPixel<TPixel>
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

        public PixelColorBlendingMode BlendMode { get; set; }

        // %
        public int Opacity { get; set; }

        public bool InvertAlpha { get; set; }

        public bool MaskAlpha { get; set; } // true if it's an inner effect

        public override void ComposeLayer(Image<TPixel> target, Image<TPixel> source)
        {
            if (!this.Enabled) return;

            var size = source.Size();
            var origin = GetOrigin(this.Angle, this.Distance);

            using (var layer = source.Clone())
            {
                if (this.InvertAlpha) { _InvertAlpha(layer); }

                layer.Mutate(dc => dc
                    .Tint(new Rgba32(this.Color))
                    .GaussianBlur(this.BlurRadius)
                    .PowerAlpha(Intensity)
                );

                target.Mutate(dc => dc.DrawImage(layer, origin, this.BlendMode, (float)this.Opacity / 100.0f));
            }
        }

        protected static Point GetOrigin(int angle, int dist)
        {
            if (dist == 0) return Point.Empty;

            var a = (Math.PI * (double)angle) / 180.0;
            var x = Math.Cos(a) * dist;
            var y = -Math.Sin(a) * dist;

            return new Point((int)x, (int)y);
        }        

        private static void _InvertAlpha(Image<TPixel> layer)
        {
            for (int y = 0; y < layer.Height; ++y)
            {
                for (int x = 0; x < layer.Width; ++x)
                {
                    var c = layer[x, y].ToVector4();

                    c.W = 1 - c.W;

                    var cc = default(TPixel); cc.PackFromVector4(c);

                    layer[x, y] = cc;
                }
            }

        }

        private static void _TransferAlpha(Image<TPixel> target, Image<TPixel> source)
        {
            for (int y = 0; y < target.Height; ++y)
            {
                for (int x = 0; x < target.Width; ++x)
                {
                    var c = target[x, y].ToVector4();

                    c.W = source[x, y].ToVector4().W;

                    var cc = default(TPixel); cc.PackFromVector4(c);
                    target[x, y] = cc;
                }
            }
        }
    }




    
    

    

    // https://www.adobe.com/devnet-apps/photoshop/fileformatashtml/PhotoshopFileFormats.htm#50577409_31889
    public class BevelEffect<TPixel> : CommonLayerEffect<TPixel> where TPixel : struct, IPixel<TPixel>
    {
        private readonly ConvolutionEffect<TPixel> _HighLight = new ConvolutionEffect<TPixel>();
        private readonly ConvolutionEffect<TPixel> _Shadow = new ConvolutionEffect<TPixel>();

        public int BevelStyle { get; set; } // this should be an enum        

        public bool UpOrDown { get; set; }

        public override void ComposeLayer(Image<TPixel> target, Image<TPixel> source)
        {
            if (!this.Enabled) return;

            _Shadow.ComposeLayer(target, source);
            _HighLight.ComposeLayer(target, source);            
        }
    }


    
}
