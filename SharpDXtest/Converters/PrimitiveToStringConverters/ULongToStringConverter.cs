using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace Editor
{
    [ValueConversion(typeof(ulong), typeof(string))]
    public sealed class ULongToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ulong res;
            if (!ulong.TryParse(((string)value).Replace(',', '.'), out res))
                return Binding.DoNothing;
            return res;
        }
    }
}