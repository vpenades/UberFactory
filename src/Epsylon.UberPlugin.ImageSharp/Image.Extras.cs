﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using Epsylon.ImageSharp.Procedural;

namespace Epsylon.UberPlugin
{
    using UberFactory;

    using PIXEL32 = Rgba32;
    using IMAGE32 = Image<Rgba32>;


    [SDK.Icon("🏴"), SDK.Title("Solid Color"), SDK.TitleFormat("{0} Solid Color")]
    [SDK.ContentNode("CreateSolidColor")]    
    public sealed class ImageSharpCreateSolidColor : ImageFilter
    {
        [SDK.InputValue("Width")]
        [SDK.Title("W"), SDK.Group("Size")]
        [SDK.Minimum(1), SDK.Default(256)]
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

    [SDK.Icon("🏁"), SDK.Title("Checkers"), SDK.TitleFormat("{0} Checkers")]
    [SDK.ContentNode("CreateCheckers")]    
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
        [SDK.Title("Odd"), SDK.Group(99)]
        [SDK.Default((UInt32)0xffffffff)]
        [SDK.ViewStyle("ColorPicker")]
        public UInt32 OddColor { get; set; }

        [SDK.InputValue("EvenColor")]
        [SDK.Title("Even"), SDK.Group(99)]
        [SDK.Default((UInt32)0xff000000)]
        [SDK.ViewStyle("ColorPicker")]
        public UInt32 EvenColor { get; set; }

        protected override IMAGE32 Evaluate()
        {
            var img = new IMAGE32(this.Width, this.Height);

            img.Mutate(dc => dc.FillCheckers(this.CellWidth, this.CellHeight, new PIXEL32(OddColor), new PIXEL32(EvenColor)));

            return img;
        }
    }

    [SDK.Icon("🏳️‍🌈"), SDK.Title("Noise"), SDK.TitleFormat("{0} Noise")]
    [SDK.ContentNode("CreatePerlinNoise")]    
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
        [SDK.Default("RsaT4f7")]
        public String RandomSeed { get; set; }

        [SDK.InputValue("Scale")]
        [SDK.Title("Scale"), SDK.Group("Noise")]
        [SDK.Minimum(2), SDK.Default(16)]
        public float Scale { get; set; }

        [SDK.InputValue("Octaves")]
        [SDK.Title("Octaves"), SDK.Group("Noise")]
        [SDK.Minimum(1), SDK.Default(8)]
        public int Octaves { get; set; }

        [SDK.InputValue("Persistence")]
        [SDK.Title("Persistence"), SDK.Group("Noise")]
        [SDK.Minimum(0), SDK.Default(50), SDK.Maximum(100)]
        [SDK.ViewStyle("Slider")]
        public int Persistence { get; set; }

        [SDK.InputNode("Gradient")]
        [SDK.Title("Gradient"), SDK.Group("Tint")]
        public PIXEL32[] Gradient { get; set; }

        protected override IMAGE32 Evaluate()
        {
            var p = (float)Persistence;

            using (var noise = new Image<HalfSingle>(Width, Height))
            {
                noise.Mutate(dc => dc.FillPerlinNoise(this.Scale, 0, this.Octaves, p / 100.0f, this.RandomSeed.GetRandomSeedHash()));

                return noise.CloneWithLookupTable(Gradient);
            }
        }
    }

    [SDK.Icon("🏳️‍🌈"), SDK.Title("Mandelbrot"), SDK.TitleFormat("{0} Mandelbrot")]
    [SDK.ContentNode("CreateMandelbrot")]
    public sealed class ImageSharpCreateMandelbrot : ImageFilter
    {
        [SDK.InputValue("Width")]
        [SDK.Title("W"), SDK.Group("Size")]
        [SDK.Minimum(1), SDK.Default(256)]
        public int Width { get; set; }

        [SDK.InputValue("Height")]
        [SDK.Title("H"), SDK.Group("Size")]
        [SDK.Minimum(1), SDK.Default(256)]
        public int Height { get; set; }

        [SDK.InputValue("OffsetX")]
        [SDK.Title("OffsetX"), SDK.Group("Fractal")]        
        public double OffsetX { get; set; }

        [SDK.InputValue("OffsetY")]
        [SDK.Title("OffsetY"), SDK.Group("Fractal")]
        public double OffsetY { get; set; }

        [SDK.InputValue("Scale")]
        [SDK.Title("Scale"), SDK.Group("Fractal")]
        [SDK.Minimum(1), SDK.Default(1000)]
        public double Scale { get; set; }

        [SDK.InputValue("Iterations")]
        [SDK.Title("Iterations"), SDK.Group("Fractal")]
        [SDK.Minimum(1),SDK.Default(100)]
        public int Iterations { get; set; }

        [SDK.InputNode("Gradient")]
        [SDK.Title("Gradient"), SDK.Group("Tint")]
        public PIXEL32[] Gradient { get; set; }

        protected override IMAGE32 Evaluate()
        {
            using (var noise = new Image<HalfSingle>(Width, Height))
            {                
                var ox = 0.5 + OffsetX/1000;
                var oy = 0.5 + OffsetX/1000;
                var ss = Scale / 1000;

                noise.Mutate(dc => dc.FillMandelbrot(ox,oy,ss, this.Iterations));

                return noise.CloneWithLookupTable(Gradient);
            }
        }
    }

    

    

    [SDK.Icon(Constants.ICON_IMAGE), SDK.Title("Metadata from Image"), SDK.TitleFormat("{0} Metadata")]
    [SDK.ContentNode("ImageSharpMetadataToText")]    
    public sealed class ImageSharpMetadataToText : SDK.ContentFilter<String>
    {
        [SDK.InputNode("Image")]
        public IImageInfo Image { get; set; }

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
                sb.AppendLine();
                sb.AppendLine("EXIF");
                sb.AppendLine();                

                foreach (var exifval in exif.Values)
                {
                    try
                    {
                        var valTxt = exifval.ToString();
                        sb.AppendLine($"{exifval.Tag} = {valTxt}");
                    }
                    catch(Exception ex)
                    {
                        sb.AppendLine($"{exifval.Tag} = {ex.Message}");
                    }
                    
                }
            }

            if (icc != null)
            {
                sb.AppendLine();
                sb.AppendLine("ICC");
                sb.AppendLine();

                foreach (var iccval in icc.Entries)
                {
                    sb.AppendLine($"{iccval.Signature} {iccval.ToString()}");
                }
            }

            if (ipps != null)
            {
                sb.AppendLine();
                sb.AppendLine("IPPS");
                sb.AppendLine();

                foreach (var iprop in ipps)
                {
                    sb.AppendLine($"{iprop.Name} = {iprop.Value}");
                }
            }

            return sb.ToString();
        }
    }

    [SDK.Icon(Constants.ICON_IMAGE), SDK.Title("Mime64 from Image"), SDK.TitleFormat("{0} Mime64 Image")]
    [SDK.ContentNode("ImageSharpToMime64")]
    public sealed class ImageSharpToMime64 : SDK.ContentFilter<String>
    {
        [SDK.InputNode("Image")]
        public IMAGE32 Image { get; set; }
        
        [SDK.InputValue("UriHeader")]
        [SDK.Title("Uri Header")]
        public Boolean UriHeader { get; set; }
        
        [SDK.InputNode("Encoder")]
        [SDK.Title("Encoder")]
        public EncoderAgent Encoder { get; set; }        

        protected override string Evaluate()
        {
            if (Image == null || Encoder == null) return null;            

            var bytes = Encoder.ToBytes(Image);

            Image.Dispose(); // end of life for this image

            var sb = new StringBuilder();

            if (UriHeader)
            {
                if (Encoder.Extension == "PNG") sb.Append("data:image/png;base64,");
                if (Encoder.Extension == "JPG") sb.Append("data:image/jpeg;base64,");
                if (Encoder.Extension == "GIF") sb.Append("data:image/gif;base64,");
            }

            sb.Append(Convert.ToBase64String(bytes));

            return sb.ToString();
        }
    }


    [SDK.Icon(Constants.ICON_IMAGE), SDK.Title("Alpha Mask from Image"), SDK.TitleFormat("{0} Alpha Mask")]
    [SDK.ContentNode("ImageSharpToAlphaMask")] public sealed class ImageSharpToAlphaMask : SDK.ContentFilter<Image<Alpha8>>
    {
        [SDK.InputNode("Image")]
        public IMAGE32 Image { get; set; }        

        protected override Image<Alpha8> Evaluate()
        {
            if (Image == null) return null;

            var mask = new Image<Alpha8>(Image.Width, Image.Height);

            float alphaFunc(System.Numerics.Vector4 v) => ((v.X + v.Y + v.Z) / 3.0f);

            for (int y=0; y < mask.Height; ++y)
            {
                for (int x = 0; x < mask.Width; ++x)
                {
                    var c = Image[x, y].ToVector4();

                    mask[x, y] = new Alpha8(alphaFunc(c));
                }
            }

            Image.Dispose();

            return mask;
        }
    }



    
    [SDK.Icon("🏙"), SDK.Title("Processing Substrate"), SDK.TitleFormat("{0} Substrate")]
    [SDK.ContentNode("ProcessingSubstrate")] public sealed class ImageSharpComplexificationSubstrate : ImageFilter
    {
        
        [SDK.Title("W"), SDK.Group("Size")]
        [SDK.Default(256)]
        [SDK.InputValue("Width")] public int Width { get; set; }
        
        [SDK.Title("H"), SDK.Group("Size")]
        [SDK.Default(256)]
        [SDK.InputValue("Height")] public int Height { get; set; }
        
        [SDK.Title("Seed"), SDK.Group("Noise")]
        [SDK.Default("RsaT4f7")]
        [SDK.InputValue("RandomSeed")] public String RandomSeed { get; set; }
        
        [SDK.Title("Iterations"), SDK.Group("Noise")]
        [SDK.Minimum(0), SDK.Default(500), SDK.Maximum(5000)]
        [SDK.InputValue("Iterations")] public int Iterations { get; set; }
        
        [SDK.Title("Palette")]
        [SDK.InputNode("Palette")] public PIXEL32[] Palette { get; set; }

        protected override IMAGE32 Evaluate()
        {
            var size = new SixLabors.Primitives.Size(Width, Height);

            var result = new IMAGE32(size.Width, size.Height);

            using (var target = new IMAGE32(size.Width, size.Height))
            {
                var substrate = Epsylon.ImageSharp.Procedural.Processing.Substrate.Create(target, RandomSeed.GetRandomSeedHash(), Palette);

                for (int i = 0; i < Iterations; ++i)
                {                    
                    this.SetProgressPercent(i * 100 / Iterations);

                    substrate.DrawStep();
                }

                result.Mutate
                    (
                    dc =>
                    {
                        dc.Fill(Rgba32.White);
                        dc.DrawImage(target, 1);
                    }
                    );

                return result;
            }
        }
    }
}
