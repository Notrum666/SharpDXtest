﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace Editor
{
    [ValueConversion(typeof(char), typeof(string))]
    public sealed class CharToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            char res;
            if (!char.TryParse(((string)value).Replace(',', '.'), out res))
                return Binding.DoNothing;
            return res;
        }
    }
}