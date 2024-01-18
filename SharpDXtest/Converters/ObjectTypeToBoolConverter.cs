using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

using Engine.BaseAssets.Components;

namespace Editor
{
    [ValueConversion(typeof(object), typeof(bool))]
    public sealed class ObjectTypeToBoolConverter : IValueConverter
    {
        public Type Type { get; set; }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.GetType().IsSameOrSubclassOf(Type);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}