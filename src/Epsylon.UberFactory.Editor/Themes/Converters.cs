using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Epsylon.UberFactory.Themes
{

    sealed class BooleanToBrushConverter : IValueConverter
    {
        public System.Windows.Media.SolidColorBrush OddBrush { get; set; }
        public System.Windows.Media.SolidColorBrush EvenBrush { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Boolean)
            {
                var v = value is Boolean ? (Boolean)value : false;

                return v ? EvenBrush : OddBrush;
            }

            if (value is UInt32 clr)
            {
                var a = (int)(clr / (256 * 256 * 256)) & 255;
                var r = (int)(clr / (256 * 256)) & 255;
                var g = (int)(clr / (256)) & 255;
                var b = (int)(clr) & 255;

                var c = System.Windows.Media.Color.FromArgb((Byte)a, (Byte)r, (Byte)g, (Byte)b);

                return new System.Windows.Media.SolidColorBrush(c);
            }

            return EvenBrush;            
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

}
