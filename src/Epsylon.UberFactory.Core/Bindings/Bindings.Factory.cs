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
            foreach (var binding in bindings.OfType<Bindings.ValueBinding>()) { binding.CopyValueToInstance(); }

            if (dependencyEvaluator == null) return;

            foreach (var binding in bindings.OfType<Bindings.DependencyBinding>()) { binding.EvaluateAndAssign(dependencyEvaluator); }            
        }

        public static IEnumerable<Bindings.MemberBinding> CreateBindings(this SDK.ContentObject source, IPropertyProvider properties)
        {
            if (source == null) return Enumerable.Empty<Bindings.MemberBinding>();
            if (properties == null) throw new ArgumentNullException(nameof(source));

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
            if (attribute is SDK.InputNodeAttribute nodeAttribute)
            {
                return Bindings.DependencyBinding.Create(bindDesc, nodeAttribute.IsCollection);
            }

            if (attribute is SDK.InputValueAttribute)
            {
                return Bindings.ValueBinding.Create(bindDesc);
            }

            return new Bindings.InvalidBinding(bindDesc);
        }

        #endregion        
    }
}
