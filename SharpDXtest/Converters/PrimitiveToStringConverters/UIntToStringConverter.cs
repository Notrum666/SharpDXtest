using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace Editor
{
    [ValueConversion(typeof(uint), typeof(string))]
    public sealed class UIntToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            uint res;
            if (!uint.TryParse(((string)value).Replace(',', '.'), out res))
                return Binding.DoNothing;
            return res;
        }
    }
}