using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Epsylon.UberFactory.Themes.Controls
{

    /// <summary>
    /// A Special button that requires you to click twice to perform the click action.
    /// </summary>
    /// <remarks>
    /// This button can be useful in cases like Removing/deleting actions that cannot be undone.
    /// </remarks>
    public class SafeButton : Button
    {
        // this is used to store the default foreground brush when we replace it with red.
        private Brush _ForegroundBackup;

        public static readonly DependencyProperty CommandIsReadyProperty = DependencyProperty.Register
            (
            nameof(CommandIsReady),
            typeof(bool),
            typeof(SafeButton),
            new FrameworkPropertyMetadata(false,_Update)
            );

        public bool CommandIsReady
        {
            get { return (bool)GetValue(CommandIsReadyProperty); }
            set { SetValue(CommandIsReadyProperty, value); }
        }        

        private static void _Update(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SafeButton ctrl) ctrl._Update(e);
        }

        private void _Update(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == CommandIsReadyProperty)
            {
                var oldval = (bool)e.OldValue;
                var newval = (bool)e.NewValue;

                if (oldval == false && newval == true) { _ForegroundBackup = Foreground; Foreground = Brushes.Red; }
                if (oldval == true && newval == false) { Foreground = _ForegroundBackup; }
            }
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            CommandIsReady = false;
        }        

        protected override void OnClick()
        {
            if (CommandIsReady == false)
            {
                CommandIsReady = true;

                return;
            }

            CommandIsReady = false;

            base.OnClick();
        }


    }
}
