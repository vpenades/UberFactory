
using System;
using System.Collections.Generic;
using System.Text;

using SixLabors.Primitives;
using SixLabors.ImageSharp.Primitives;
using SixLabors.ImageSharp.MetaData.Profiles.Exif;

using V2 = System.Numerics.Vector2;


namespace Epsylon.ImageSharp.Procedural
{
    // https://github.com/ianare/exif-samples
    // https://www.exif.org/category/samples

    // https://github.com/SixLabors/ImageSharp/blob/03bd0211a9e928e7c22e814f61809a673f938606/src/ImageSharp/Processing/Transforms/TransformHelpers.cs#L22

    // these tags essentially define a "main" point area within the image, making them ideal to define basic origin and solid.

    public class SubjectInfo
    {
        public static SubjectInfo FromProfile(ExifProfile profile)
        {
            if (profile == null) return null;

            var sinfo = new SubjectInfo();

            // https://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/subjectdistance.html
            var tag = profile.GetValue(ExifTag.SubjectDistance);

            if (tag != null)
            {
                if (tag.IsArray || tag.DataType != ExifDataType.Rational) throw new ArgumentException();

                var value = (Rational)tag.Value;

                if (value.Numerator == 0) sinfo.Distance = float.NaN;
                else if (value.Numerator == 0xffffffff) sinfo.Distance = float.PositiveInfinity;
                else sinfo.Distance = value.ToDouble();
            }

            https://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/subjectdistancerange.html
            tag = profile.GetValue(ExifTag.SubjectDistanceRange);

            if (tag != null)
            {
                if (tag.IsArray || tag.DataType != ExifDataType.Short) throw new ArgumentException();

                var value = (ushort)tag.Value;
                sinfo.Range = (DistanceRange)value;                
            }

            // https://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/subjectarea.html
            tag = profile.GetValue(ExifTag.SubjectArea);            

            if (tag != null)
            {
                if (!tag.IsArray || tag.DataType != ExifDataType.Short) throw new ArgumentException();

                var array = (ushort[])tag.Value;

                // point
                sinfo.Center = new Point(array[0], array[1]);

                // circle
                if (array.Length == 3) sinfo.Diameter = array[2];

                // rectangle
                if (array.Length == 4) sinfo.Size = new Size(array[2],array[3]);

                return sinfo;
            }

            // https://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/subjectlocation.html
            tag = profile.GetValue(ExifTag.SubjectLocation);

            if (tag != null)
            {
                if (!tag.IsArray || tag.DataType != ExifDataType.Short) throw new ArgumentException();

                var array = (ushort[])tag.Value;

                // point
                if (array.Length == 2) sinfo.Center = new Point(array[0], array[1]);

                return sinfo;
            }

            return null;
        }

        public SubjectInfo()
        {
            Distance = Double.NaN;
        }

        /// <summary>
        /// Subject center, in pixels
        /// </summary>
        public Point Center { get; set; }

        /// <summary>
        /// If defined, is the diameter of the circle in pixels
        /// </summary>
        public int? Diameter { get; set; }

        /// <summary>
        /// If defined, is the size of the rectangle, in pixels
        /// </summary>
        public Size? Size { get; set; }

        /// <summary>
        /// measured distance of the subject from the camera (in metres?)
        /// </summary>
        /// <remarks>
        /// Default Value: NaN (meaning Unknown value)
        /// Special Value: Positive Infinity.
        /// </remarks>
        public Double Distance { get; set; }

        public enum DistanceRange
        {
            Unknown = 0,
            Macro = 1,
            CloseView = 2,
            DistantView = 3
        }

        public DistanceRange Range { get; set; }

        public Rectangle Bounds
        {
            get
            {
                // rectangle
                if (Size.HasValue)
                {
                    var tl = new Point(Center.X - Size.Value.Width / 2, Center.Y - Size.Value.Height / 2);
                    return new Rectangle(tl,Size.Value);
                }

                // circle
                if (Diameter.HasValue)
                {
                    var tl = new Point(Center.X - Diameter.Value / 2, Center.Y - Diameter.Value / 2);
                    return new Rectangle(tl, new Size(Diameter.Value) );
                }

                return new Rectangle(Center, new Size(1, 1));                
            }
        }

        public RectangleF BoundsFloat
        {
            get
            {
                // rectangle
                if (Size.HasValue)
                {
                    var tl = PointF.Subtract(Center,Size.Value) *0.5f;
                    return new RectangleF(tl, Size.Value);
                }

                // circle
                if (Diameter.HasValue)
                {
                    var tl = PointF.Subtract(Center , new SizeF(Diameter.Value,Diameter.Value)) * 0.5f;
                    return new RectangleF(tl, new Size(Diameter.Value));
                }

                return new RectangleF(Center, new SizeF(1, 1));
            }
        }

        public void WriteTo(ExifProfile profile)
        {
            if (!double.IsNaN(Distance) && Distance > 0)
            {
                var value = Distance == Double.PositiveInfinity ? new Rational(0xffffffff, 1) : new Rational(Distance);

                profile.ClearValue(ExifTag.SubjectDistance);
                profile.SetValue(ExifTag.SubjectDistance, value);
            }

            if (Range != DistanceRange.Unknown)
            {
                profile.ClearValue(ExifTag.SubjectDistanceRange);
                profile.SetValue(ExifTag.SubjectDistanceRange, (ushort)Range);
            }

            profile.ClearValue(ExifTag.SubjectLocation);
            profile.ClearValue(ExifTag.SubjectArea);

            if (Size.HasValue)
            {
                var rect = new ushort[] { (ushort)Center.X, (ushort)Center.Y, (ushort)Size.Value.Width, (ushort)Size.Value.Height };
                profile.SetValue(ExifTag.SubjectArea, rect);
            }
            else if (Diameter.HasValue)
            {
                var circle = new ushort[] { (ushort)Center.X, (ushort)Center.Y, (ushort)Diameter.Value };
                profile.SetValue(ExifTag.SubjectArea, circle);
            }
            else
            {
                var origin = new ushort[] { (ushort)Center.X, (ushort)Center.Y };
                profile.SetValue(ExifTag.SubjectArea, origin);
            }

            if (true) // apparently SubjectLocation is outdated and superseded by SubjectArea, but we write it anyway for backwards compatibility
            {
                var origin = new ushort[] { (ushort)Center.X, (ushort)Center.Y };
                profile.SetValue(ExifTag.SubjectLocation, origin);
            }
            
        }
    }
}
