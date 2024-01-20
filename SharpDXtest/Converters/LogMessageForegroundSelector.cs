using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

using Engine;

namespace Editor
{
    [ValueConversion(typeof(LogMessage), typeof(Brush))]
    public sealed class LogMessageForegroundSelector : IValueConverter
    {
        public Brush InfoBrush { get; set; }
        public Brush WarningBrush { get; set; }
        public Brush ErrorBrush { get; set; }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((LogMessage)value).Type switch
            {
                LogType.Info => InfoBrush,
                LogType.Warning => WarningBrush,
                LogType.Error => ErrorBrush,
                _ => null
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}