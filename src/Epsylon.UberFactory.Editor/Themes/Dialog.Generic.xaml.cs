using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Epsylon.UberFactory.Themes
{    
    public partial class GenericDialog : Window
    {
        #region lifecycle

        public static GenericDialog Create<T>(Window parentWindow, string title, Object data) where T : FrameworkElement
        {
            if (parentWindow == null) parentWindow = Application.Current.MainWindow;

            var dlg = new GenericDialog
            {
                Owner = parentWindow,
                Icon = parentWindow.Icon,
                Title = title
            };

            dlg.SetDataTemplate(typeof(T));
            dlg.DataSource = data;

            return dlg;
        }

        private GenericDialog()
        {
            InitializeComponent();

            myExtraButtons.ItemsSource = _ExtraCommands;        
        }        

        #endregion

        #region dependency properties

        private static void _DataSourceChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            var ctrl = dependencyObject as GenericDialog; ctrl?._DataSourceChanged(eventArgs.OldValue, eventArgs.NewValue);
        }

        public static readonly DependencyProperty DataSourceProperty = DependencyProperty.Register
            (
            nameof(DataSource),
            typeof(Object),
            typeof(GenericDialog),
            new PropertyMetadata(null, _DataSourceChanged)
            );

        public Object DataSource
        {
            get { return this.GetValue(DataSourceProperty); }
            set { this.SetValue(DataSourceProperty, value); }
        }

        public static readonly DependencyProperty DataTemplateProperty = DependencyProperty.Register
            (
            nameof(DataTemplate),
            typeof(DataTemplate),
            typeof(GenericDialog),
            new PropertyMetadata(null)
            );

        public DataTemplate DataTemplate
        {
            get { return this.GetValue(DataTemplateProperty) as DataTemplate; }
            set { this.SetValue(DataTemplateProperty, value); }
        }

        #endregion

        #region data

        private readonly System.Collections.ObjectModel.ObservableCollection<_CommandButton> _ExtraCommands = new System.Collections.ObjectModel.ObservableCollection<_CommandButton>();

        #endregion

        #region API

        public void SetDataTemplate(Type t)
        {
            if (t == null) throw new ArgumentNullException(nameof(t));
            if (!typeof(FrameworkElement).IsAssignableFrom(t)) throw new ArgumentException("Must be derived from FrameworkElement",nameof(t));

            var dt = new DataTemplate() { VisualTree = new FrameworkElementFactory(t) };

            this.DataTemplate = dt;
        }

        private void _DataSourceChanged(Object oldValue, Object newValue)
        {
            _ExtraCommands.Clear();

            if (newValue != null)
            {
                var commands = newValue?.GetType()
                    .GetTypeInfo()
                    .DeclaredProperties
                    .Where(pinfo => typeof(ICommand).IsAssignableFrom(pinfo.PropertyType))
                    .ToArray();

                if (commands != null)
                {
                    foreach (var cmd in commands)
                    {
                        var nameAttr = cmd.GetCustomAttribute<System.ComponentModel.DisplayNameAttribute>();
                        var name = nameAttr != null ? nameAttr.DisplayName : cmd.Name;
                        var value = cmd.GetValue(newValue) as ICommand;
                        if (value == null) continue;

                        _ExtraCommands.Add(_CommandButton.Create(name, value));
                    }
                }
            }

            if (false)
            {
                _ExtraCommands.Add(_CommandButton.CreateCancel(() => _Close(false)));
                _ExtraCommands.Add(_CommandButton.CreateAccept(() => _Close(true)));
            }
            else
            {
                _ExtraCommands.Add(_CommandButton.CreateClose(() => _Close(true)));
            }
        }

        private void _Close(bool result)
        {
            this.DialogResult = result;

            this.Close();
        }        

        #endregion

        #region support classes        

        internal sealed class _CommandButton
        {
            public static _CommandButton CreateCancel(Action action)
            {
                return new _CommandButton("Cancel", new RelayCommand(action), false, true);
            }

            public static _CommandButton CreateClose(Action action)
            {
                return new _CommandButton("Close", new RelayCommand(action), true, false);
            }

            public static _CommandButton CreateAccept(Action action)
            {
                return new _CommandButton("Ok", new RelayCommand(action), true, false);
            }

            public static _CommandButton Create(string name, ICommand cmd)
            {
                return new _CommandButton(name, cmd, false, false);
            }

            private _CommandButton(string dn, ICommand c, bool isDefault, bool isCancel)
            {
                this.DisplayName = dn;
                this.Command = c;
                this.IsDefault = isDefault;
                this.IsCancel = isCancel;
            }

            public string DisplayName { get; private set; }

            public ICommand Command { get; private set; }

            public bool IsDefault { get; private set; }

            public bool IsCancel { get; private set; }
        }

        #endregion
    }

    
}
