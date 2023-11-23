using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace Editor
{
    [ValueConversion(typeof(DateTime), typeof(string))]
    public sealed class DateTimeConverter : IValueConverter
    {
        public string Format { get; set; }
        public DateTimeConverter()
        {
            Format = "HH:mm:ss";
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not DateTime)
                return DependencyProperty.UnsetValue;

            return ((DateTime)value).ToString(Format);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
