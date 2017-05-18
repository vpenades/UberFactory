using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    public abstract class BindableBase : INotifyPropertyChanged
    {
        #if DEBUG

        // this instance is used to compare if two instances (typically MVC proxy objects) are the same, since they're regenerated quite often

        private readonly Guid _DEBUG_InstanceId = Guid.NewGuid();

        public Guid DEBUG_InstanceId { get { return _DEBUG_InstanceId; } }

        #endif

        #region INotifyPropertyChanged Members

        protected virtual void RaiseChanged(params string[] ps)
        {
            if (PropertyChanged == null) return;

            if (ps == null || ps.Length == 0) { PropertyChanged(this, new PropertyChangedEventArgs(null)); return; }

            foreach (var p in ps) PropertyChanged(this, new PropertyChangedEventArgs(p));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }

    public class RelayCommand<T> : System.Windows.Input.ICommand
    {
        // http://stackoverflow.com/questions/21821762/relaycommand-wont-execute-on-button-click

        #region Lifecycle

        public RelayCommand(Action<T> execute) : this(execute, null) { }

        public RelayCommand(Action<T> execute, Predicate<T> canExecute)
        {
            _Execute = execute ?? throw new ArgumentNullException("execute");

            _CanExecute = canExecute;
        }

        #endregion

        #region Data

        private readonly Action<T> _Execute = null;
        private readonly Predicate<T> _CanExecute = null;

        #endregion

        #region ICommand Members

        public event EventHandler CanExecuteChanged
        {
            add { System.Windows.Input.CommandManager.RequerySuggested += value; }
            remove { System.Windows.Input.CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) { return _CanExecute == null ? true : _CanExecute((T)parameter); }

        //[System.Diagnostics.DebuggerStepThrough]
        public void Execute(object parameter) { _Execute((T)parameter); }

        #endregion
    }

    public class RelayCommand : System.Windows.Input.ICommand
    {
        // http://stackoverflow.com/questions/21821762/relaycommand-wont-execute-on-button-click

        #region Lifecycle

        public RelayCommand(Action execute) : this(execute, null) { }

        public RelayCommand(Action execute, Predicate<Object> canExecute)
        {
            _Execute = execute ?? throw new ArgumentNullException("execute");

            _CanExecute = canExecute;
        }

        #endregion

        #region Data

        private readonly Action _Execute = null;
        private readonly Predicate<Object> _CanExecute = null;

        #endregion

        #region ICommand Members

        public event EventHandler CanExecuteChanged
        {
            add { System.Windows.Input.CommandManager.RequerySuggested += value; }
            remove { System.Windows.Input.CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) { return _CanExecute == null ? true : _CanExecute(parameter); }

        //[System.Diagnostics.DebuggerStepThrough]
        public void Execute(object parameter) { _Execute(); }

        #endregion
    }


    public class NamedRelayCommand : RelayCommand
    {
        public NamedRelayCommand(string name, Action execute) : this(name, execute, null) { }

        public NamedRelayCommand(string name, Action execute, Predicate<Object> canExecute) : base(execute, canExecute) { Name = name; }

        public String Name { get; private set; }
    }

    public class NamedRelayCommand<T> : RelayCommand<T>
    {
        public NamedRelayCommand(string name, Action<T> execute) : this(name, execute, null) { }

        public NamedRelayCommand(string name, Action<T> execute, Predicate<T> canExecute) : base(execute, canExecute) { Name = name; }

        public String Name { get; private set; }
    }
}
