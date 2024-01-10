using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Editor
{
    [ValueConversion(typeof(object), typeof(bool))]
    public sealed class ObjectToBoolConverter : IValueConverter
    {
        public bool NullIsTrue { get; set; } = false;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return NullIsTrue ? value is null : value is not null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}