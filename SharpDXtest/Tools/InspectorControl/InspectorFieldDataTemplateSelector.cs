using System;
using System.Windows;
using System.Windows.Controls;

namespace Editor
{
    public class InspectorFieldDataTemplateSelector : DataTemplateSelector
    {
        private static DataTemplate notImplementedDataTemplate = null;
        public static DataTemplate NotImplementedDataTemplate => notImplementedDataTemplate ?? (notImplementedDataTemplate = (DataTemplate)Application.Current.FindResource("FieldNotImplementedTemplate"));

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            Type fieldType = ((FieldViewModel)item).TargetField.FieldType;
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