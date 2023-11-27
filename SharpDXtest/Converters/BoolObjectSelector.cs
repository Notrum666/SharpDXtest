using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Editor
{
    [ValueConversion(typeof(bool), typeof(object))]
    public sealed class BoolObjectSelector : IValueConverter
    {
        public object TrueValue { get; set; }
        public object FalseValue { get; set; }

        public BoolObjectSelector()
        {

        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not bool)
                return null;
            return (bool)value ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Equals(value, TrueValue))
                return true;
            if (Equals(value, FalseValue))
                return false;
            return DependencyProperty.UnsetValue;
        }
    }
}