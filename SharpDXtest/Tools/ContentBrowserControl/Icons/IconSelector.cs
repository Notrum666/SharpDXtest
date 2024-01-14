using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

using Engine;

namespace Editor
{
    [ValueConversion(typeof(object), typeof(Uri))]
    public sealed class IconSelector : IValueConverter
    {
        public IconSelector()
        {

        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new Uri(value switch
            {
                ContentBrowserFolderViewModel _ => "\\Tools\\ContentBrowserControl\\Icons\\FolderIcon.png",
                ContentBrowserAssetViewModel _ => "\\Tools\\ContentBrowserControl\\Icons\\FileIcon.png",
                _ => "\\Tools\\ContentBrowserControl\\Icons\\FileIcon.png"
            }, UriKind.Relative);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}