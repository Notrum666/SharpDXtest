using System;
using System.Globalization;
using System.Windows.Data;

namespace Editor
{
    [ValueConversion(typeof(float), typeof(string))]
    public sealed class FloatToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            float res;
            if (!float.TryParse(((string)value).Replace(',', '.'), out res))
                return Binding.DoNothing;
            return res;
        }
    }
}