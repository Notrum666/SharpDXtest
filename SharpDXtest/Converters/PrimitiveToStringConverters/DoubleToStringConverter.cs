using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace Editor
{
    [ValueConversion(typeof(double), typeof(string))]
    public sealed class DoubleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double res;
            if (!double.TryParse(((string)value).Replace(',', '.'), out res))
                return Binding.DoNothing;
            return res;
        }
    }
}