using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;
using Engine;
using System.Collections.Generic;

namespace Editor
{
    [ValueConversion(typeof(object[]), typeof(Visibility))]
    public sealed class LogMessageVisibiliySelector : IMultiValueConverter
    {
        public LogMessageVisibiliySelector()
        {

        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Dictionary<LogType, Func<bool>>)values[0])[(LogType)values[1]]() ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("LogMessageVissibilitySelector MultiValueConverter should be used only with one-way binding.");
        }
    }
}
