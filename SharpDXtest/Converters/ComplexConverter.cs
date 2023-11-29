using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace Editor
{
    public sealed class ComplexConverter : IValueConverter
    {
        public List<IValueConverter> ConvertersSequence { get; set; }

        public ComplexConverter()
        {
            ConvertersSequence = new List<IValueConverter>();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (ConvertersSequence.Count == 0)
                throw new Exception("ComplexConverter must have at least one converter in sequence.");

            for (int i = 0; i < ConvertersSequence.Count; i++)
            {
                Type target = targetType;
                if (i != ConvertersSequence.Count - 1)
                {
                    object[] attributes = ConvertersSequence[i + 1].GetType().GetCustomAttributes(typeof(ValueConversionAttribute), false);
                    if (attributes.Length > 1)
                        target = (attributes[0] as ValueConversionAttribute).SourceType;
                    else
                        target = null;
                }
                value = ConvertersSequence[i].Convert(value, target, parameter, culture);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (ConvertersSequence.Count == 0)
                throw new Exception("ComplexConverter must have at least one converter in sequence.");

            for (int i = ConvertersSequence.Count - 1; i >= 0; i--)
            {
                Type target = targetType;
                if (i != 0)
                {
                    object[] attributes = ConvertersSequence[i - 1].GetType().GetCustomAttributes(typeof(ValueConversionAttribute), false);
                    if (attributes.Length > 1)
                        target = (attributes[0] as ValueConversionAttribute).SourceType;
                    else
                        target = null;
                }
                value = ConvertersSequence[i].ConvertBack(value, target, parameter, culture);
            }

            return value;
        }
    }
}