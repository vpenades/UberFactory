using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Helpers;
using SixLabors.ImageSharp.MetaData;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;

namespace Epsylon.ImageSharp.Procedural
{
    public static class _MetadataExtensions
    {
        // TODO: Trick: to store offsets or vector data that is not affected by the image resizes,
        // we can store values in the range 0-1 , unfortunately, crop and padding can't be solved.

        #region API

        public static int IndexOf(this IList<ImageProperty> properties, string key)
        {
            for (int i = 0; i < properties.Count; ++i)
            {
                if (properties[i].Name == key)
                {
                    return i;
                }
            }

            return -1;
        }

        public static T GetValue<T>(this IList<ImageProperty> properties, string key, T defval) where T : IConvertible
        {
            var p = properties.FirstOrDefault(item => item.Name == key);

            return p == null ? defval : (T)System.Convert.ChangeType(p.Value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
        }

        public static void SetValue<T>(this IList<ImageProperty> properties, string key, T val, T defval) where T : IConvertible
        {
            var idx = properties.IndexOf(key);

            if (val.Equals(defval) && idx >= 0)
            {
                properties.RemoveAt(idx);
                return;
            }

            var p = new ImageProperty(key, val.ToString(System.Globalization.CultureInfo.InvariantCulture));

            if (idx < 0) properties.Add(p);
            else properties[idx] = p;
        }

        #endregion

        #region internals

        private const string _InternalPropertyPrefix = "{9578CA40-2C8B-40AB-AB22-5A7B5777D861}-";
        private const string _PixelOffsetX = _InternalPropertyPrefix + "PixelOffsetX";
        private const string _PixelOffsetY = _InternalPropertyPrefix + "PixelOffsetY";
        private const string _Opacity = _InternalPropertyPrefix + "Opacity";
        private const string _BlendMode = _InternalPropertyPrefix + "BlendMode";       

        public static Point GetInternalPixelOffset(this ImageMetaData metadata)
        {
            var x = metadata.Properties.GetValue<int>(_PixelOffsetX, 0);
            var y = metadata.Properties.GetValue<int>(_PixelOffsetY, 0);

            return new Point(x, y);
        }

        public static void SetInternalPixelOffset(this ImageMetaData metadata, int x, int y)
        {
            metadata.SetInternalPixelOffset(new Point(x, y));
        }

        public static void SetInternalPixelOffset(this ImageMetaData metadata, Point offset)
        {
            metadata.Properties.SetValue<int>(_PixelOffsetX, offset.X, 0);
            metadata.Properties.SetValue<int>(_PixelOffsetY, offset.Y, 0);
        }        


        public static float GetInternalOpacity(this ImageMetaData metadata)
        {
            return metadata.Properties.GetValue<float>(_Opacity, 1);
        }

        public static void SetInternalOpacity(this ImageMetaData metadata, float value)
        {
            metadata.Properties.SetValue<float>(_Opacity, value, 1);
        }

        public static PixelBlenderMode GetInternalBlendMode(this ImageMetaData metadata)
        {
            var val = (int)PixelBlenderMode.Normal;

            val = metadata.Properties.GetValue<int>(_BlendMode,val);

            return (PixelBlenderMode)val;
        }

        public static void SetInternalBlendMode(this ImageMetaData metadata, PixelBlenderMode value)
        {
            var defval = (int)PixelBlenderMode.Normal;

            metadata.Properties.SetValue<float>(_BlendMode, (int)value, defval);
        }

        #endregion

        #region Exif

        public static IImageProcessingContext<TPixel> SetSubjectInfo<TPixel>(this IImageProcessingContext<TPixel> source, SubjectInfo sinfo) where TPixel : struct, IPixel<TPixel>
        {
            return source.Apply(img => img.SetSubjectInfo(sinfo));
        }

        public static SubjectInfo GetSubjectInfo(this IImage image)
        {
            return SubjectInfo.FromProfile(image?.MetaData?.ExifProfile);            
        }

        public static void SetSubjectInfo(this IImage image, SubjectInfo sinfo)
        {
            sinfo.WriteTo(image.UseExifProfile());
        }

        #endregion

        #region extras

        /// <summary>
        /// Resizes metadata values associated to the image, to match the bitmap resizing
        /// </summary>
        /// <remarks>
        /// Should be applied BEFORE the actual bitmap resizing
        /// </remarks>
        public static IImageProcessingContext<TPixel> ResizeMetaData<TPixel>(this IImageProcessingContext<TPixel> source, int newWidth, int newHeight) where TPixel : struct, IPixel<TPixel>
        {
            return source.ApplyProcessor
            ( image =>
                {
                    var offset = image.MetaData.GetInternalPixelOffset();

                    offset.X = offset.X * newWidth / image.Width;
                    offset.Y = offset.Y * newHeight / image.Height;

                    image.MetaData.SetInternalPixelOffset(offset);
                }
            );
        }

        #endregion
    }



    
}
