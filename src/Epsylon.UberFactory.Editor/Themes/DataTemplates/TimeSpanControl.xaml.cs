using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Epsylon.UberFactory.Themes.DataTemplates
{    
    public partial class TimeSpanControl : UserControl, INotifyPropertyChanged
    {
        public TimeSpanControl()
        {
            InitializeComponent();
        }

        private void RaisePropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public TimeSpan Value
        {
            get { return (TimeSpan)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        private static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(TimeSpan), typeof(TimeSpanControl),
            new FrameworkPropertyMetadata(TimeSpan.Zero, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, _OnValueChanged));

        private static void _OnValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var control = obj as TimeSpanControl;
            var newValue = (TimeSpan)e.NewValue;
            
            control.Milliseconds = newValue.Milliseconds;
            control.Seconds = newValue.Seconds;
            control.Minutes = newValue.Minutes;
            control.Hours = newValue.Hours;
            control.Days = newValue.Days;
        }


        private int _Days;
        private int _Hours;
        private int _Minutes;
        private int _Seconds;
        private int _Milliseconds;

        public int Days
        {
            get { return _Days; }
            set
            {
                if (value == _Days) return;

                _Days = value;

                RaisePropertyChanged(nameof(Days));

                var v = Value;
                Value = new TimeSpan(_Days, v.Hours, v.Minutes, v.Seconds, v.Milliseconds);
            }
        }

        public int Hours
        {
            get { return _Hours; }
            set
            {
                value = value.Clamp(-23, 23);

                if (value == _Hours) return;

                _Hours = value;

                RaisePropertyChanged(nameof(Hours));

                var v = Value;
                Value = new TimeSpan(v.Days, _Hours, v.Minutes, v.Seconds, v.Milliseconds);
            }
        }

        public int Minutes
        {
            get { return _Minutes; }
            set
            {
                value = value.Clamp(-59, 59);

                if (value == _Minutes) return;

                _Minutes = value;

                RaisePropertyChanged(nameof(Minutes));

                var v = Value;
                Value = new TimeSpan(v.Days, v.Hours, _Minutes, v.Seconds, v.Milliseconds);
            }
        }

        public int Seconds
        {
            get { return _Seconds; }
            set
            {
                value = value.Clamp(-59, 59);

                if (value == _Seconds) return;

                _Seconds = value;

                RaisePropertyChanged(nameof(Seconds));

                var v = Value;
                Value = new TimeSpan(v.Days, v.Hours, v.Minutes, _Seconds,v.Milliseconds);
            }
        }

        public int Milliseconds
        {
            get { return _Milliseconds; }
            set
            {
                value = value.Clamp(-999, 999);

                if (value == _Milliseconds) return;

                _Milliseconds = value;

                RaisePropertyChanged(nameof(Milliseconds));

                var v = Value;
                Value = new TimeSpan(v.Days, v.Hours, v.Minutes, v.Seconds, _Milliseconds);
            }
        }
    }
}
