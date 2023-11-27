using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace Editor
{
    [ValueConversion(typeof(short), typeof(string))]
    public sealed class ShortToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            short res;
            if (!short.TryParse(((string)value).Replace(',', '.'), out res))
                return Binding.DoNothing;
            return res;
        }
    }
}