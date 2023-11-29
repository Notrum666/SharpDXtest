using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Editor
{
    public class InspectorFieldDataTemplateSelector : DataTemplateSelector
    {
        private static DataTemplate notImplementedDataTemplate = null;
        public static DataTemplate NotImplementedDataTemplate
        {
            get => notImplementedDataTemplate ?? (notImplementedDataTemplate = (DataTemplate)Application.Current.FindResource("FieldNotImplementedTemplate"));
        }
        private static DataTemplate nullDataTemplate = null;
        public static DataTemplate NullDataTemplate
        {
            get => nullDataTemplate ?? (nullDataTemplate = (DataTemplate)Application.Current.FindResource("FieldTypeNullTemplate"));
        }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            Type fieldType = ((FieldViewModel)item).TargetType;
            if (fieldType == null)
                return NullDataTemplate;

            int closestDistance = int.MaxValue;
            FieldDataTemplate fieldDataTemplate = null;
            foreach (FieldDataTemplate template in InspectorControl.FieldDataTemplates)
            {
                int distance;
                if (template.Predicate(fieldType) && (distance = fieldType.GetInheritanceDistance(template.TargetType)) != -1 && distance < closestDistance)
                { 
                    closestDistance = distance;
                    fieldDataTemplate = template;
                }
            }

            if (fieldDataTemplate == null)
                return NotImplementedDataTemplate;
            //    throw new NotImplementedException("No suitable data template was found for type " + fieldType.Name);

            return fieldDataTemplate;
        }
    }
}
