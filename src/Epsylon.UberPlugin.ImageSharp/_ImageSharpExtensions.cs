﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SixLabors.ImageSharp;

namespace Epsylon.UberPlugin
{
    using COLOR = SixLabors.ImageSharp.Rgba32;
    using IMAGE = SixLabors.ImageSharp.Image;
    using IMAGE32 = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.Rgba32>;

    using SKIAIMAGE = SkiaSharp.SKBitmap;
    

    static class _ImageSharpExtensions
    {
        public static String GetPickFileFilter(this IEnumerable<SixLabors.ImageSharp.Formats.IImageFormat> formats)
        {
            var extensions = SixLabors.ImageSharp.Configuration.Default.ImageFormats
                .SelectMany(item => item.FileExtensions)
                .Select(item => "*." + item)
                .ToArray();

            var exts = string.Join(";", extensions);

            return $"Bitmap Files|{exts}";            
        }

        public static Object CreatePreview(this IMAGE32 image, UberFactory.SDK.PreviewContext context)
        {
            if (image == null) return null;

            var files = context.CreateMemoryFile("preview.png");

            files.WriteStream(s => image.SaveAsPng(s));

            return files;
        }

        public static double WidthInInches(this IMAGE32 img) { return (double)img.Width / img.MetaData.HorizontalResolution; }

        public static double HeightInInches(this IMAGE32 img) { return (double)img.Height / img.MetaData.VerticalResolution; }

        /*
        public static ImageSharp.Formats.IImageFormat GetImageformat(this ImageSharp.IEncoderOptions options)
        {
            HL.IconPro.Lib.Core.IconFormat.Use();

            var fmts = ImageSharp.Configuration.Default.ImageFormats;
            
            if (options is ImageSharp.Formats.IPngEncoderOptions) return fmts.OfType<ImageSharp.Formats.PngFormat>().FirstOrDefault();
            if (options is ImageSharp.Formats.IBmpEncoderOptions) return fmts.OfType<ImageSharp.Formats.BmpFormat>().FirstOrDefault();
            if (options is ImageSharp.Formats.JpegEncoderOptions) return fmts.OfType<ImageSharp.Formats.JpegFormat>().FirstOrDefault();
            if (options is HL.IconPro.Lib.Core.IconEncoderOptions) return fmts.OfType<HL.IconPro.Lib.Core.IconFormat>().FirstOrDefault();

            throw new NotSupportedException();
        }*/


        public static Byte[] GetBufferBytes(this IMAGE32 srcImage)
        {
            if (srcImage == null) return null;            

            var srcBytes = new Byte[srcImage.Width * srcImage.Height * 4];

            for (int y = 0; y < srcImage.Height; ++y)
            {
                for (int x = 0; x < srcImage.Width; ++x)
                {
                    var pix = srcImage[x,y];

                    var idx = (y * srcImage.Width + x) * 4;

                    srcBytes[idx + 0] = pix.B;
                    srcBytes[idx + 1] = pix.G;
                    srcBytes[idx + 2] = pix.R;
                    srcBytes[idx + 3] = pix.A;
                }
            }            

            return srcBytes;
        }

        

        public static IMAGE32 RenderNoise(int width, int height, INoiseGenerator noiseGen, float scale)
        {
            var img = new IMAGE32(width, height);
            
            for (int y = 0; y < img.Height; ++y)
            {
                for (int x = 0; x < img.Width; ++x)
                {
                    float xx = x;
                    float yy = y;

                    xx /= scale;
                    yy /= scale;

                    var k = noiseGen.GetSample(xx, yy);

                    k = k * 0.5f + 0.5f;

                    if (k < 0) k = 0;
                    if (k > 1) k = 1;

                    var c = default(COLOR);

                    c.A = 255;
                    c.R = c.G = c.B = (Byte)(k * 255.0f);

                    img[x,y] = c;
                }
            }
            

            return img;
        }


        
        public static SKIAIMAGE Render(this SkiaSharp.Extended.Svg.SKSvg svg)
        {
            // https://github.com/vvvv/SVG/issues/291

            // http://stackoverflow.com/questions/42183229/can-i-render-svg-to-png-using-skiasharp

            var bitmap = new SKIAIMAGE((int)svg.CanvasSize.Width, (int)svg.CanvasSize.Height);

            using (var canvas = new SkiaSharp.SKCanvas(bitmap))
            {
                canvas.Clear();
                canvas.DrawPicture(svg.Picture);
                canvas.Flush();
                canvas.Save();                
            }

            return bitmap;
        }


        public static IMAGE32 ToImageSharp(this SKIAIMAGE src)
        {
            var dst = new IMAGE32(src.Width, src.Height);
            dst.MetaData.HorizontalResolution = 96;
            dst.MetaData.VerticalResolution = 96;

            src.LockPixels();

            
            for(int y=0; y < dst.Height; ++y)
            {
                for (int x=0; x < dst.Width; ++x)
                {
                    var c = src.GetPixel(x, y);

                    dst[x,y] = new COLOR(c.Red, c.Green, c.Blue, c.Alpha);
                }
            }
            

            src.UnlockPixels();

            return dst;
            
        }

        


        public static void Flatten(this IMAGE32 target, IMAGE32 source, int sx, int sy,  Func<COLOR, COLOR, Single, COLOR> func, int opacity)
        {
            if (opacity < 0 || opacity > 100) throw new ArgumentOutOfRangeException(nameof(opacity));

            if (opacity == 0) return;

            float fOpacity = (float)opacity / 100.0f;

            
            var dx = sx;
            var dy = sy;

            if (dx < 0) dx = 0;
            if (dy < 0) dy = 0;

            sx = -sx;
            sy = -sy;

            if (sx < 0) sx = 0;
            if (sy < 0) sy = 0;

            var w = Math.Min(target.Width-dx, source.Width-sx);
            var h = Math.Min(target.Height-dy, source.Height-sy);

            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    var d = target[x+dx, y + dy];
                    var s = source[x+sx, y + sy];

                    if (s.A > 0) d = func(d, s, fOpacity);

                    target[x+dx, y+dy] = d;
                }
            }

            
        }
        

        public static IMAGE32 RenderText(this SixLabors.Fonts.FontFamily ffamily, string text, float fsize, float padding, COLOR color, SixLabors.ImageSharp.Drawing.TextGraphicsOptions options)
        {
            // http://sixlabors.com/2017/04/08/watermarks/

            if (string.IsNullOrEmpty(text)) return null;

            var font = new SixLabors.Fonts.Font(ffamily, fsize);
            var roptions = new SixLabors.Fonts.RendererOptions(font, 96);
            var size = SixLabors.Fonts.TextMeasurer.Measure(text, roptions);

            var w = (int)Math.Ceiling(size.Width + padding * 2);
            var h = (int)Math.Ceiling(size.Height + padding * 2);
            var img = new IMAGE32(w, h);

            img.Mutate( dc => dc.DrawText(text, font, color, new System.Numerics.Vector2(padding, padding), options) );

            return img;
        }
    }
}