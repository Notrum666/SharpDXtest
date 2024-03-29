﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace Editor
{
    [ValueConversion(typeof(ushort), typeof(string))]
    public sealed class UShortToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ushort res;
            if (!ushort.TryParse(((string)value).Replace(',', '.'), out res))
                return Binding.DoNothing;
            return res;
        }
    }
}