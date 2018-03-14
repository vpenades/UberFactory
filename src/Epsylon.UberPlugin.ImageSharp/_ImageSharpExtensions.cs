
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SixLabors.Primitives;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Drawing;
using SixLabors.ImageSharp.Processing.Text;
using SixLabors.ImageSharp.Processing.Quantization;
using SixLabors.ImageSharp.Processing.Transforms.Resamplers;


namespace Epsylon.UberPlugin
{
    using Epsylon.ImageSharp.Procedural;

    using POINT = Point;
    using COLOR = Rgba32;
    using IMAGE = Image;
    using IMAGE32 = Image<Rgba32>;
    

    public enum Resampler
    {
        NearestNeighbor,
        Box, Triangle, Bicubic,
        Spline,
        CatmullRom,
        Hermite,
        Robidoux, RobidouxSharp,
        Welch,
        Lanczos2, Lanczos3, Lanczos5, Lanczos8,
        MitchellNetravali,
    }

    static class _ImageSharpExtensions
    {
        public static int AsInteger(this SixLabors.ImageSharp.MetaData.Profiles.Exif.ExifValue value)
        {
            if (value == null) return 0;
            if (value.DataType == SixLabors.ImageSharp.MetaData.Profiles.Exif.ExifDataType.Byte) return (byte)value.Value;
            if (value.DataType == SixLabors.ImageSharp.MetaData.Profiles.Exif.ExifDataType.SignedShort) return (short)value.Value;
            if (value.DataType == SixLabors.ImageSharp.MetaData.Profiles.Exif.ExifDataType.SignedLong) return (int)((long)value.Value);

            return 0;
        }


        public static POINT GetExifPositionOffset(this IMAGE32 image)
        {
            // value should be rational64u            

            var r = (ushort)image.MetaData.ExifProfile.GetValue(SixLabors.ImageSharp.MetaData.Profiles.Exif.ExifTag.ResolutionUnit).Value;

            // r == 1 pixels
            // r == 2 inch
            // r == 3 cm

            var x = (SixLabors.ImageSharp.MetaData.Profiles.Exif.Rational)image.MetaData.ExifProfile.GetValue(SixLabors.ImageSharp.MetaData.Profiles.Exif.ExifTag.XPosition).Value;
            var y = (SixLabors.ImageSharp.MetaData.Profiles.Exif.Rational)image.MetaData.ExifProfile.GetValue(SixLabors.ImageSharp.MetaData.Profiles.Exif.ExifTag.YPosition).Value;

            return new POINT((int)x.ToDouble(), (int)y.ToDouble());
        }

        public static void SetExifPositionOffset(this IMAGE32 image, POINT offset)
        {
            // value should be rational64u

            var x = SixLabors.ImageSharp.MetaData.Profiles.Exif.Rational.FromDouble(offset.X);
            var y = SixLabors.ImageSharp.MetaData.Profiles.Exif.Rational.FromDouble(offset.Y);

            image.MetaData.ExifProfile.SetValue(SixLabors.ImageSharp.MetaData.Profiles.Exif.ExifTag.XPosition, x);
            image.MetaData.ExifProfile.SetValue(SixLabors.ImageSharp.MetaData.Profiles.Exif.ExifTag.YPosition, y);
        }

        


        public static IQuantizer GetInstance(this QuantizationMode mode)
        {
            if (mode == QuantizationMode.Octree) return new OctreeQuantizer<Rgba32>();
            if (mode == QuantizationMode.Palette) return new PaletteQuantizer<Rgba32>();
            if (mode == QuantizationMode.Wu) return new WuQuantizer<Rgba32>();

            throw new NotImplementedException();
        }

        public static COLOR WithAlpha(this COLOR color, int alpha)
        {
            alpha = Math.Min(alpha, 255);
            alpha = Math.Max(alpha, 0);

            color.A = (Byte)alpha;

            return color;
        }

        public static IResampler GetInstance(this Resampler mode)
        {
            if (mode == Resampler.NearestNeighbor) return new NearestNeighborResampler();

            if (mode == Resampler.Box) return new BoxResampler();
            if (mode == Resampler.Triangle) return new TriangleResampler();
            if (mode == Resampler.Bicubic) return new BicubicResampler();

            if (mode == Resampler.Spline) return new SplineResampler();
            if (mode == Resampler.CatmullRom) return new CatmullRomResampler();
            if (mode == Resampler.Hermite) return new HermiteResampler();

            if (mode == Resampler.Robidoux) return new RobidouxResampler();
            if (mode == Resampler.RobidouxSharp) return new RobidouxSharpResampler();
            if (mode == Resampler.Welch) return new WelchResampler();

            if (mode == Resampler.Lanczos2) return new Lanczos2Resampler();
            if (mode == Resampler.Lanczos3) return new Lanczos3Resampler();            
            if (mode == Resampler.Lanczos5) return new Lanczos5Resampler();
            if (mode == Resampler.Lanczos8) return new Lanczos8Resampler();

            if (mode == Resampler.MitchellNetravali) return new MitchellNetravaliResampler();

            throw new NotImplementedException();
        }

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

            if (true)
            {
                var sinfo = image.GetSubjectInfo();                

                if (sinfo != null)
                {
                    var l = new PointF[2];

                    l[0] = sinfo.Center + new Size(3, 0);
                    l[1] = sinfo.Center + new Size(3, 0);
                    image.Mutate(dc => dc.DrawLines(COLOR.Red, 1, l) );

                    l[0] = sinfo.Center + new Size(0, 3);
                    l[1] = sinfo.Center - new Size(0, 3);
                    image.Mutate(dc => dc.DrawLines(COLOR.Red, 1, l));                    

                    if (sinfo.Size.HasValue)
                    {
                        image.Mutate(dc => dc.DrawPolygon(COLOR.Red, 1, sinfo.BoundsFloat.Get4Points()));                        
                    }

                }                
            }

            var date = DateTime.Now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
            var time = DateTime.Now.ToString("hhmmss", System.Globalization.CultureInfo.InvariantCulture);

            var files = context.CreateMemoryFile($"preview-{date}-{time}.png");

            files.WriteStream(s => image.SaveAsPng(s));

            image.Dispose();

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
        

        public static IMAGE32 RenderText(this SixLabors.Fonts.FontFamily ffamily, string text, float fsize, float padding, COLOR color, TextGraphicsOptions options)
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


        public static void MutatePixels(this IMAGE32 image, Func<COLOR,COLOR> pixelfunc)
        {
            for(int y=0; y < image.Height; ++y)
            {
                for(int x=0; x < image.Width; ++x)
                {
                    image[x, y] = pixelfunc(image[x, y]);
                }
            }
        }
    }
}
