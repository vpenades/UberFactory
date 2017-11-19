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
        #region constants

        internal const string VIEWTEMPLATE_INVALID = "BindingView_Invalid";
        internal const string VIEWTEMPLATE_TEXTBOX = "BindingView_TextBox";
        internal const string VIEWTEMPLATE_CHECKBOX = "BindingView_CheckBox";
        internal const string VIEWTEMPLATE_COMBOBOX = "BindingView_ComboBox";        

        internal const string VIEWTEMPLATE_SLIDER = "BindingView_Slider";

        internal const string VIEWTEMPLATE_COLORPICKER = "BindingView_ColorPicker";

        internal const string VIEWTEMPLATE_TIMEBOX = "BindingView_TimeBox";
        internal const string VIEWTEMPLATE_DATEBOX = "BindingView_DateBox";

        internal const string VIEWTEMPLATE_PATHPICKER = "BindingView_PathPicker";

        #endregion

        #region lifecycle

        public static MemberBinding Create(Description bindDesc)
        {
            var propertyType = bindDesc.Member.GetAssignType();

            if (propertyType == null) throw new ArgumentNullException(nameof(propertyType));

            if (propertyType == typeof(String))
            {
                var vb = new InputValueBinding<String>(bindDesc);

                if (vb.ViewTemplate == VIEWTEMPLATE_PATHPICKER) return SourceFilePickBinding.CreateFilePick(bindDesc);

                return vb;
            }

            if (propertyType.GetTypeInfo().IsEnum) return new InputEnumerationBinding(bindDesc);

            if (propertyType == typeof(Boolean)) return new InputValueBinding<Boolean>(bindDesc);

            if (propertyType == typeof(Char)) return new InputValueBinding<Char>(bindDesc);

            if (propertyType == typeof(SByte)) return new InputValueBinding<SByte>(bindDesc);
            if (propertyType == typeof(Int16)) return new InputValueBinding<Int16>(bindDesc);
            if (propertyType == typeof(Int32)) return new InputValueBinding<Int32>(bindDesc);
            if (propertyType == typeof(Int64)) return new InputValueBinding<Int64>(bindDesc);

            if (propertyType == typeof(Byte)) return new InputValueBinding<Byte>(bindDesc);
            if (propertyType == typeof(UInt16)) return new InputValueBinding<UInt16>(bindDesc);
            if (propertyType == typeof(UInt32)) return new InputValueBinding<UInt32>(bindDesc);
            if (propertyType == typeof(UInt64)) return new InputValueBinding<UInt64>(bindDesc);

            if (propertyType == typeof(Single)) return new InputValueBinding<Single>(bindDesc);
            if (propertyType == typeof(Double)) return new InputValueBinding<Double>(bindDesc);
            if (propertyType == typeof(Decimal)) return new InputValueBinding<Decimal>(bindDesc);

            if (propertyType == typeof(TimeSpan)) return new InputTimeSpanBinding(bindDesc);
            if (propertyType == typeof(DateTime)) return new InputValueBinding<DateTime>(bindDesc);  
            // TODO: DateTimeOffset

            // TODO: Guid
            // TODO: Version

            return new InvalidBinding(bindDesc);
        }

        internal ValueBinding(Description pvd) : base(pvd)
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

        public Action ClearValueCmd => ClearValue;

        public bool HasValue => _Properties.Contains(SerializationKey);        

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

        public abstract void CopyValueToInstance();

        public void ClearValue()
        {
            _Properties.Clear(SerializationKey);

            CopyValueToInstance();

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

            CopyValueToInstance();

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

            CopyValueToInstance();

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
    public sealed class InputValueBinding<T> : ValueBinding where T : IConvertible, IComparable, IComparable<T>
    {
        #region lifecycle

        internal InputValueBinding(Description pvd) : base(pvd)
        {            
            if (typeof(T) == typeof(Object)) throw new ArgumentException(nameof(T), "Not Supported");
        }

        #endregion

        #region properties        

        public override string ViewTemplate
        {
            get
            {
                if (typeof(T) == typeof(bool)) return VIEWTEMPLATE_CHECKBOX;

                var ctrl = this.GetMetaDataValue<String>("ViewStyle", "TextBox");

                if (ctrl == "PathPicker") return VIEWTEMPLATE_PATHPICKER;

                if (ctrl == "ComboBox") return VIEWTEMPLATE_COMBOBOX;

                if (typeof(T) == typeof(DateTime)) return VIEWTEMPLATE_DATEBOX;                

                if (ctrl == "TextBox") return VIEWTEMPLATE_TEXTBOX;                

                if (typeof(T) == typeof(String)) return VIEWTEMPLATE_INVALID;

                if (ctrl == "Slider") return VIEWTEMPLATE_SLIDER; // TODO: check Minimum & Maximum must be defined

                if (typeof(T) != typeof(UInt32)) return VIEWTEMPLATE_INVALID;

                if (ctrl == "ColorPicker") return VIEWTEMPLATE_COLORPICKER;

                return VIEWTEMPLATE_INVALID;
            }
        }

        public bool IsMultiLine => this.GetMetaDataValue<Boolean>("MultiLine", false);

        public int MaxTextLines => 5;

        public T[] AvailableValues => _GetTypeAvailableValues();

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

        public override void CopyValueToInstance() { SetInstanceValue(Value); }

        private static Object _GetTypeMinimumValue()
        {
            if (typeof(T) == typeof(String)) return null; // specifically required for generic Clamp

            if (typeof(T) == typeof(Boolean)) return false;
            if (typeof(T) == typeof(Char)) return Char.MinValue;

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

            if (typeof(T) == typeof(TimeSpan)) return TimeSpan.MinValue;
            if (typeof(T) == typeof(DateTime)) return DateTime.MinValue;
            if (typeof(T) == typeof(DateTimeOffset)) return DateTimeOffset.MinValue;
            
            System.Diagnostics.Debug.Assert(false,$"Invalid type {typeof(T)}");

            return default(T);
        }

        private static Object _GetTypeMaximumValue()
        {
            if (typeof(T) == typeof(String)) return null; // specifically required for generic Clamp

            if (typeof(T) == typeof(Boolean)) return true;
            if (typeof(T) == typeof(Char)) return Char.MaxValue;

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
            
            if (typeof(T) == typeof(TimeSpan)) return TimeSpan.MaxValue;
            if (typeof(T) == typeof(DateTime)) return DateTime.MaxValue;
            if (typeof(T) == typeof(DateTimeOffset)) return DateTimeOffset.MaxValue;

            System.Diagnostics.Debug.Assert(false, $"Invalid type {typeof(T)}");

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
    public sealed class InputEnumerationBinding : ValueBinding
    {
        #region lifecycle

        internal InputEnumerationBinding(Description pvd) : base(pvd) { }

        #endregion

        #region properties        

        public override string ViewTemplate => VIEWTEMPLATE_COMBOBOX;

        public Enum[] AvailableValues => _GetTypeAvailableValues();

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

        public override void CopyValueToInstance() { SetInstanceValue(Value); }

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

    [System.Diagnostics.DebuggerDisplay("Enum {SerializationKey} = {Value}")]
    public sealed class InputTimeSpanBinding : ValueBinding
    {
        #region lifecycle

        internal InputTimeSpanBinding(Description pvd) : base(pvd) { }

        #endregion

        #region properties        

        public override string ViewTemplate => VIEWTEMPLATE_TIMEBOX;        

        public TimeSpan Value
        {
            get { return new TimeSpan(this.GetValue<long>()); }
            set
            {
                if (value < Minimum) value = Minimum;
                if (value > Maximum) value = Maximum;
                this.SetValue<long>(value.Ticks);
            }
        }

        public TimeSpan Minimum => this.GetMetaDataValue<TimeSpan>("Minimum", TimeSpan.MinValue);

        public TimeSpan Maximum => this.GetMetaDataValue<TimeSpan>("Maximum", TimeSpan.MaxValue);

        #endregion

        #region API

        public override void CopyValueToInstance()
        {
            SetInstanceValue(Value);
        }

        #endregion
    }


    [System.Diagnostics.DebuggerDisplay("File {SerializationKey} = {FileName}")]
    public sealed class SourceFilePickBinding : ValueBinding
    {
        #region lifecycle

        public static SourceFilePickBinding CreateFilePick(Description pvd) { return new SourceFilePickBinding(pvd); }

        public static SourceFilePickBinding CreateDirectoryPick(Description pvd) { return new SourceFilePickBinding(pvd); }

        private SourceFilePickBinding(Description pvd) : base(pvd) { }

        #endregion

        #region properties

        public override string ViewTemplate => VIEWTEMPLATE_PATHPICKER;        

        public String Value
        {
            get
            {
                var path = this.GetValue<String>();
                return path == null ? null : this.DataContext.BuildContext.GetSourceAbsolutePath(path);
            }
            set
            {
                var rpath = value == null ? null : this.DataContext.BuildContext.GetRelativeToSource(value);
                this.SetValue(rpath);
            }
        }

        public string FilePath => Value;

        public string FileName => System.IO.Path.GetFileName(FilePath);

        #endregion

        #region API         

        public override void CopyValueToInstance() { SetInstanceValue(Value); }

        public string GetFileFilter() { return this.GetMetaDataValue<String>("Filter", "All Files|*.*"); }

        #endregion
    }
    
    

    

        
}
