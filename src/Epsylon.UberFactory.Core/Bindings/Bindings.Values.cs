using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory.Bindings
{
    public abstract class ValueBinding : MemberBinding
    {
        #region lifecycle

        public ValueBinding(Description pvd) : base(pvd)
        {
            _Properties = pvd.Properties;
        }

        #endregion

        #region data

        private readonly IPropertyProvider _Properties;        

        internal ValueBinding[] _AllValueBindings;

        #endregion        

        #region properties

        public virtual string ViewTemplate => "BindingView_Invalid";

        public Action ClearValueCmd { get { return ClearValue; } }

        public bool HasValue { get { return _Properties.Contains(SerializationKey); } }

        // this is the text value to be displayed to know the fallback value after clearing the current value                    
        public String DisplayDefaultValue
        {
            get
            {
                var txt = GetDefaultValue<String>();
                if (txt == null) return "NULL";

                if (txt.Length > 100) txt = txt.Substring(0, 256) + "...";

                if (DataType == typeof(String)) txt = "“" + txt + "”";

                return txt;
            }
        }

        #endregion

        #region API

        // Getter methods read the value from the serialization DOM object
        // Setter methods write the value first to the serialization DOM object, and then to the node Instance.
        // * values are never read from the node instance *

        public void SetEvaluatedResult(Object value) { SetInstanceValue(value); }

        public abstract void CopyToInstance();

        public void ClearValue()
        {
            _Properties.Clear(SerializationKey);

            CopyToInstance();

            RaiseChanged();
        }

        protected TValue GetValue<TValue>() where TValue : IConvertible
        {
            var defval = default(TValue);            

            defval = this.GetMetaDataValue("Default", defval);

            return _Properties.GetValue(SerializationKey, defval.ConvertToString<TValue>()).ConvertToValue<TValue>();
        }

        protected TValue GetDefaultValue<TValue>() where TValue : IConvertible
        {
            var defval = this.GetMetaDataValue("Default", default(TValue));

            return _Properties.GetDefaultValue(SerializationKey, defval.ConvertToString<TValue>()).ConvertToValue<TValue>();
        }

        protected void SetValue<TValue>(TValue value) where TValue : IConvertible
        {
            var changed = _Properties.SetValue(SerializationKey, value.ConvertToString());

            CopyToInstance();

            if (changed) RaiseValueChanged();
        }

        protected TValue[] GetArray<TValue>() where TValue : IConvertible
        {
            var defval = this.GetMetaDataValue("Default", default(TValue[]));

            var defarray = defval?.Select(item => item.ConvertToString<TValue>())
                    .ToArray();

            var array = _Properties.GetArray(SerializationKey, defarray);

            return array?
                .Select(item => item.ConvertToValue<TValue>())
                .ToArray();
        }

        protected void SetArray<TValue>(TValue[] array) where TValue : IConvertible
        {
            var xarray = array?.Select(item => item.ConvertToString())
                .ToArray();

            var changed = _Properties.SetArray(SerializationKey, xarray);

            CopyToInstance();

            if (changed) RaiseValueChanged();
        }


        protected Byte[] GetBytes(Byte[] defval)
        {
            var val = GetValue<String>();
            if (string.IsNullOrWhiteSpace(val)) return defval;
            return System.Convert.FromBase64String(val);
        }

        protected void SetBytes(Byte[] val)
        {
            var text = val == null ? null : System.Convert.ToBase64String(val);
            SetValue(text);
        }

        protected TEnum GetEnum<TEnum>(TEnum defval) where TEnum : struct
        {
            if (!typeof(TEnum).GetTypeInfo().IsEnum) throw new ArgumentException(nameof(TEnum));

            var txtval = GetValue<String>();

            return Enum.TryParse<TEnum>(txtval, out TEnum r) ? r : defval;
        }

        protected void SetEnum<TEnum>(TEnum val) where TEnum : struct
        {
            if (!typeof(TEnum).GetTypeInfo().IsEnum) throw new ArgumentException(nameof(TEnum));

            SetValue<String>(val.ToString());
        }

        protected void RaiseValueChanged()
        {
            foreach (var vb in _AllValueBindings) vb.RaiseChanged();
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Value {SerializationKey} = {Value}")]
    public class InputValueBinding<T> : ValueBinding where T : IConvertible, IComparable, IComparable<T>
    {
        #region lifecycle

        public InputValueBinding(Description pvd) : base(pvd)
        {            
            if (typeof(T) == typeof(Object)) throw new ArgumentException(nameof(T), "Not Supported");
        }

        #endregion

        #region properties        

        public override string ViewTemplate
        {
            get
            {
                if (typeof(T) == typeof(bool)) return "BindingView_CheckBox";

                var ctrl = this.GetMetaDataValue<String>("ViewStyle", "TextBox");

                if (ctrl == "ComboBox") return "BindingView_ComboBox";

                if (typeof(T) == typeof(DateTime)) return "BindingView_DateBox";                

                if (ctrl == "TextBox") return "BindingView_TextBox";                

                if (typeof(T) == typeof(String)) return "BindingView_Invalid";

                if (ctrl == "Slider") return "BindingView_Slider"; // note, Minimum & Maximum must be defined

                if (typeof(T) != typeof(UInt32)) return "BindingView_Invalid";

                if (ctrl == "ColorPicker") return "BindingView_ColorPicker";

                return "BindingView_Invalid";
            }
        }

        public bool IsMultiLine => this.GetMetaDataValue<Boolean>("MultiLine", false);

        public int MaxTextLines => 5;

        public T[] AvailableValues { get { return _GetTypeAvailableValues(); } }

        public T Value
        {
            get { return this.GetValue<T>(); }
            set
            {
                value = value.Clamp(Minimum, Maximum);
                this.SetValue(value);
            }
        }

        public T Minimum => this.GetMetaDataValue<T>("Minimum", (T)_GetTypeMinimumValue());

        public T Maximum => this.GetMetaDataValue<T>("Maximum", (T)_GetTypeMaximumValue());

        #endregion

        #region API

        public override void CopyToInstance() { SetInstanceValue(Value); }

        private static Object _GetTypeMinimumValue()
        {
            if (typeof(T) == typeof(String)) return null;

            if (typeof(T) == typeof(Boolean)) return false;

            if (typeof(T) == typeof(SByte)) return SByte.MinValue;
            if (typeof(T) == typeof(Int16)) return Int16.MinValue;
            if (typeof(T) == typeof(Int32)) return Int32.MinValue;
            if (typeof(T) == typeof(Int64)) return Int64.MinValue;

            if (typeof(T) == typeof(Byte)) return Byte.MinValue;
            if (typeof(T) == typeof(UInt16)) return UInt16.MinValue;
            if (typeof(T) == typeof(UInt32)) return UInt32.MinValue;
            if (typeof(T) == typeof(UInt64)) return UInt64.MinValue;

            if (typeof(T) == typeof(Single)) return Single.MinValue;
            if (typeof(T) == typeof(Double)) return Double.MinValue;
            if (typeof(T) == typeof(Decimal)) return Decimal.MinValue;

            if (typeof(T) == typeof(DateTime)) return DateTime.MinValue;

            return 0;
        }

        private static Object _GetTypeMaximumValue()
        {
            if (typeof(T) == typeof(String)) return null;

            if (typeof(T) == typeof(Boolean)) return true;

            if (typeof(T) == typeof(SByte)) return SByte.MaxValue;
            if (typeof(T) == typeof(Int16)) return Int16.MaxValue;
            if (typeof(T) == typeof(Int32)) return Int32.MaxValue;
            if (typeof(T) == typeof(Int64)) return Int64.MaxValue;

            if (typeof(T) == typeof(Byte)) return Byte.MaxValue;
            if (typeof(T) == typeof(UInt16)) return UInt16.MaxValue;
            if (typeof(T) == typeof(UInt32)) return UInt32.MaxValue;
            if (typeof(T) == typeof(UInt64)) return UInt64.MaxValue;

            if (typeof(T) == typeof(Single)) return Single.MaxValue;
            if (typeof(T) == typeof(Double)) return Double.MaxValue;
            if (typeof(T) == typeof(Decimal)) return Decimal.MaxValue;

            if (typeof(T) == typeof(DateTime)) return DateTime.MaxValue;

            return default(T);
        }

        private T[] _GetTypeAvailableValues()
        {
            var t = this.DataType;

            return this.GetMetaDataValue<T[]>("Values", new T[0]);
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Enum {SerializationKey} = {Value}")]
    public class InputEnumerationBinding : ValueBinding
    {
        #region lifecycle

        public InputEnumerationBinding(Description pvd) : base(pvd) { }

        #endregion

        #region properties        

        public override string ViewTemplate => "BindingView_ComboBox";

        public Enum[] AvailableValues { get { return _GetTypeAvailableValues(); } }

        public Enum Value
        {
            get
            {
                var value = this.GetValue<String>();

                try
                {
                    return (Enum)Enum.Parse(this.DataType, value);
                }
                catch
                {
                    return (Enum)Enum.GetValues(this.DataType).GetValue(0);
                }
            }
            set { this.SetValue<String>(value.ToString()); }
        }

        #endregion

        #region API

        public override void CopyToInstance() { SetInstanceValue(Value); }

        private Enum[] _GetTypeAvailableValues()
        {
            var t = this.DataType;

            var defaultValues = Enum.GetValues(t)
                .Cast<Enum>()
                // .Select(item => item.ToString())
                .ToArray();

            return this.GetMetaDataValue<Enum[]>("Values", defaultValues);
        }

        #endregion
    }


    [System.Diagnostics.DebuggerDisplay("File {SerializationKey} = {FileName}")]
    public class SourceFilePickBinding : ValueBinding
    {
        #region lifecycle

        public static SourceFilePickBinding CreateFilePick(Description pvd) { return new SourceFilePickBinding(pvd); }

        public static SourceFilePickBinding CreateDirectoryPick(Description pvd) { return new SourceFilePickBinding(pvd); }

        private SourceFilePickBinding(Description pvd) : base(pvd) { }

        #endregion

        #region properties

        public override string ViewTemplate => "BindingView_PathPicker";

        public Action ShowPickPathDialogCmd => _PickFileDialog;

        public Uri Value
        {
            get
            {
                var path = this.GetValue<String>();
                return path == null ? null : this.DataContext.BuildContext.GetSourceAbsoluteUri(path);
            }
            set
            {
                var rpath = value == null ? null : this.DataContext.BuildContext.GetRelativeToSource(value);
                this.SetValue(rpath);
            }
        }

        public string FilePath => Value.ToFriendlySystemPath();

        public string FileName => System.IO.Path.GetFileName(FilePath);

        #endregion

        #region API

        private void _PickDirectoryDialog()
        {
            var startDir = this.DataContext.BuildContext.GetSourceAbsoluteUri("dummy.txt");

            var newDir = DialogHooks.ShowDirectoryPickerDialog(new PathString(startDir).DirectoryPath);
            if (newDir.IsEmpty) return;

            Value = new Uri(newDir, UriKind.Absolute);
        }

        private void _PickFileDialog()
        {
            var startDir = this.DataContext.BuildContext.GetSourceAbsoluteUri("dummy.txt");            

            var newFile = DialogHooks.ShowFilePickerDialog(GetFileFilter(), new PathString(startDir).DirectoryPath);
            if (newFile.IsEmpty) return;            

            Value = new Uri(newFile, UriKind.Absolute);
        }        

        public override void CopyToInstance() { SetInstanceValue(Value); }

        public string GetFileFilter() { return this.GetMetaDataValue<String>("Filter", "All Files|*.*"); }

        #endregion
    }
    
    

    

        
}
