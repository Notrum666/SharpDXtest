using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Editor
{
    [ValueConversion(typeof(double), typeof(double))]
    public sealed class SizeToFontSizeConverter : IValueConverter
    {
        public double Denominator { get; set; } = 1.0;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)value / Denominator;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)value * Denominator;
        }
    }
}