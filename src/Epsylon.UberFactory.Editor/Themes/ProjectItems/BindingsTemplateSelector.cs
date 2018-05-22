using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Epsylon.UberFactory.Themes.ProjectItems
{
    public class BindingsTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (!(container is FrameworkElement element)) return null;

            var value = _SelectTemplate(item, element);

            return value as DataTemplate;
        }

        private static Object _SelectTemplate(object item, FrameworkElement element)
        {
            if (item is ProjectVIEW.GroupedBindingsView grouped)
            {
                if (grouped.DisplayName.StartsWith("#")) return element.FindResource("BindingView_Group_Simplified");

                return element.FindResource("BindingView_Group");
            }

            // dependency types
            if (item is ProjectVIEW.SingleDependencyView xitem)
            {
                if (xitem.IsInstanced) return element.FindResource("BindingView_Dependency_Single_Instanced");

                return element.FindResource("BindingView_Dependency_Single_Empty");
            }

            if (item is ProjectVIEW.ArrayDependencyView) return element.FindResource("BindingView_Dependency_Multi");

            // Value types
            if (item is Bindings.ValueBinding valueBinding) return element.FindResource(valueBinding.ViewTemplate);

            // default
            return element.FindResource("BindingView_Invalid");
        }
    }
}
