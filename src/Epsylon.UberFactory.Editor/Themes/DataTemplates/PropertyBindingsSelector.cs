using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Epsylon.UberFactory.Themes.DataTemplates
{
    public class PropertyBindingsSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var element = container as FrameworkElement;
            if (element == null) return null;

            if (item is ProjectVIEW.GroupedBindingsView grouped)
            {
                if (grouped.DisplayName.StartsWith("#")) return element.FindResource("BindingView_Group_Simplified") as DataTemplate;

                return element.FindResource("BindingView_Group") as DataTemplate;
            }

            // dependency types
            if (item is ProjectVIEW.SingleDependencyView xitem)
            {
                if (xitem.IsInstanced) return element.FindResource("BindingView_Dependency_Single_Instanced") as DataTemplate;

                return element.FindResource("BindingView_Dependency_Single_Empty") as DataTemplate;
            }

            if (item is ProjectVIEW.ArrayDependencyView) return element.FindResource("BindingView_Dependency_Multi") as DataTemplate;            

            // Value types
            if (item is Bindings.ValueBinding valueBinding) return element.FindResource(valueBinding.ViewTemplate) as DataTemplate;

            // default
            return element.FindResource("BindingView_Invalid") as DataTemplate;
        }
    }
}
