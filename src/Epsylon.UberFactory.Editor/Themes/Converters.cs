﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Epsylon.UberFactory.Themes
{

    sealed class ConvertibleToBrushConverter : IValueConverter
    {
        public System.Windows.Media.SolidColorBrush FalseBrush { get; set; }
        public System.Windows.Media.SolidColorBrush TrueBrush { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Boolean)
            {
                var v = value is Boolean ? (Boolean)value : false;

                return v ? TrueBrush : FalseBrush;
            }

            if (value is Int32 clrs) { value = (UInt32)clrs; }

            if (value is UInt32 clru)
            {
                var a = (int)(clru / (256 * 256 * 256)) & 255;
                var b = (int)(clru / (256 * 256)) & 255;
                var g = (int)(clru / (256)) & 255;
                var r = (int)(clru) & 255;

                var c = System.Windows.Media.Color.FromArgb((Byte)a, (Byte)r, (Byte)g, (Byte)b);

                return new System.Windows.Media.SolidColorBrush(c);
            }           

            return TrueBrush;            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    sealed class RelayCommandConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Action) return new RelayCommand((Action)value);

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    sealed class TypeDisplayNameConverter : IValueConverter
    {
        // http://stackoverflow.com/questions/4185521/c-sharp-get-generic-type-name

        private static string _GetFriendlyName(Type type)
        {
            string friendlyName = type.FullName;

            if (friendlyName.StartsWith("System.")) friendlyName = friendlyName.Substring(7);

            if (type.IsGenericType)
            {
                int iBacktick = friendlyName.IndexOf('`');
                if (iBacktick > 0) friendlyName = friendlyName.Remove(iBacktick);

                friendlyName += "<";

                var args = type
                    .GetGenericArguments()
                    .Select(item => _GetFriendlyName(item))
                    .ToArray();

                friendlyName += string.Join(",", args);

                friendlyName += ">";
            }

            return friendlyName;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var t = value as Type; if (t == null) return null;

            if (t.Name == "NULL_Type") return "NULL";

            return _GetFriendlyName(t);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    sealed class BooleanToTextWrappingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bval = value is Boolean ? (Boolean)value : false;

            return bval ? System.Windows.TextWrapping.WrapWithOverflow : System.Windows.TextWrapping.NoWrap;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class FilePathConverter : IValueConverter
    {
        public enum PathPart { FullPath, Directory, FileName, Name, Extension }


        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path && parameter is PathPart arg)
            {
                if (arg == PathPart.FullPath) return System.IO.Path.GetFullPath(path);
                if (arg == PathPart.Directory) return System.IO.Path.GetDirectoryName(path);
                if (arg == PathPart.FileName) return System.IO.Path.GetFileName(path);
                if (arg == PathPart.Name) return System.IO.Path.GetFileNameWithoutExtension(path);
                if (arg == PathPart.Extension) return System.IO.Path.GetExtension(path);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
