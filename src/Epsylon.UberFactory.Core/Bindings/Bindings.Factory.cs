using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    public static class BindingsFactory
    {
        #region factory

        public static void ClearDependencyBindings(this SDK.ContentObject source, IPropertyProvider properties)
        {
            var bindings = CreateBindings(source, properties);            

            foreach (var binding in bindings.OfType<Bindings.DependencyBinding>())
            {
                binding.ClearEvaluatedResult();
            }
        }

        public static void EvaluateBindings(this SDK.ContentObject source, IPropertyProvider properties, Func<Guid, Object> dependencyEvaluator)
        {
            var bindings = CreateBindings(source, properties);
            foreach (var binding in bindings.OfType<Bindings.ValueBinding>()) { binding.CopyToInstance(); }

            if (dependencyEvaluator == null) return;

            foreach (var binding in bindings.OfType<Bindings.DependencyBinding>()) { binding.EvaluateAndAssign(dependencyEvaluator); }            
        }

        public static IEnumerable<Bindings.MemberBinding> CreateBindings(this SDK.ContentObject source, IPropertyProvider properties)
        {
            if (source == null) return Enumerable.Empty<Bindings.MemberBinding>();

            return _CreateBindings(source, tinfo => tinfo.FlattenedProperties(), properties)
                .Concat(_CreateBindings(source, tinfo => tinfo.FlattenedMethods(), properties));
        }

        private static Bindings.MemberBinding[] _CreateBindings(SDK.ContentObject source, Func<TypeInfo, IEnumerable<MemberInfo>> func, IPropertyProvider properties)
        {
            var members = func(source.GetType().GetTypeInfo());

            var bindings = new List<Bindings.MemberBinding>();

            foreach (var minfo in members)
            {
                var attrib = minfo.GetInputDescAttribute();
                if (attrib == null) continue;

                var desc = new Bindings.MemberBinding.Description()
                {
                    Member = minfo,
                    Properties = properties,
                    Target = source
                };

                var binding = _CreateBinding(attrib, desc);
                if (binding == null) continue;

                bindings.Add(binding);
            }

            if (true) // we interconnect all the VALUE bindings , so when a value of a binding is changed, all of them raise a NotifyPropertyChanged, so they can, more or less, work as a basic state machine
            {
                var valueBindings = bindings.OfType<Bindings.ValueBinding>().ToArray();

                foreach (var vb in valueBindings)
                {
                    vb._AllValueBindings = valueBindings;
                }
            }

            return bindings.ToArray();
        }

        private static Bindings.MemberBinding _CreateBinding(SDK.InputPropertyAttribute attribute, Bindings.MemberBinding.Description bindDesc)
        {
            var propertyType = bindDesc.Member.GetAssignType();            

            if (attribute is SDK.InputNodeAttribute)
            {
                var isArray = propertyType.IsArray;
                var isMulti = ((SDK.InputNodeAttribute)attribute).IsCollection;
                return isMulti && isArray ? (Bindings.MemberBinding)new Bindings.MultiDependencyBinding(bindDesc) : new Bindings.SingleDependencyBinding(bindDesc);
            }

            if (attribute is SDK.InputValueAttribute)
            {
                if (propertyType == typeof(String)) return new Bindings.InputValueBinding<String>(bindDesc);
                if (propertyType == typeof(Boolean)) return new Bindings.InputValueBinding<Boolean>(bindDesc);
                if (propertyType.GetTypeInfo().IsEnum) return new Bindings.InputEnumerationBinding(bindDesc);

                if (propertyType == typeof(SByte)) return new Bindings.InputValueBinding<SByte>(bindDesc);
                if (propertyType == typeof(Int16)) return new Bindings.InputValueBinding<Int16>(bindDesc);
                if (propertyType == typeof(Int32)) return new Bindings.InputValueBinding<Int32>(bindDesc);
                if (propertyType == typeof(Int64)) return new Bindings.InputValueBinding<Int64>(bindDesc);

                if (propertyType == typeof(Byte)) return new Bindings.InputValueBinding<Byte>(bindDesc);
                if (propertyType == typeof(UInt16)) return new Bindings.InputValueBinding<UInt16>(bindDesc);
                if (propertyType == typeof(UInt32)) return new Bindings.InputValueBinding<UInt32>(bindDesc);
                if (propertyType == typeof(UInt64)) return new Bindings.InputValueBinding<UInt64>(bindDesc);

                if (propertyType == typeof(Single)) return new Bindings.InputValueBinding<Single>(bindDesc);
                if (propertyType == typeof(Double)) return new Bindings.InputValueBinding<Double>(bindDesc);
                if (propertyType == typeof(Decimal)) return new Bindings.InputValueBinding<Decimal>(bindDesc);                

                if (propertyType == typeof(DateTime)) return new Bindings.InputValueBinding<DateTime>(bindDesc);

                
                if (propertyType == typeof(System.IO.FileInfo)) return Bindings.SourceFilePickBinding.CreateFilePick(bindDesc);
                if (propertyType == typeof(System.IO.DirectoryInfo)) return Bindings.SourceFilePickBinding.CreateDirectoryPick(bindDesc);

                // we should check metadata to decide if it's a file or a directory
                if (propertyType == typeof(Uri)) return Bindings.SourceFilePickBinding.CreateFilePick(bindDesc);
                if (propertyType == typeof(PathString)) return Bindings.SourceFilePickBinding.CreateFilePick(bindDesc);

                // Guid
                // Version
                // TimeSpan
            }

            return new Bindings.InvalidBinding(bindDesc);
        }

        #endregion        
    }
}
