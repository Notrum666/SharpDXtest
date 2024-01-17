using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

using Engine;

namespace Editor
{
    public sealed class ObjectToStringConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            Type expectedType = (Type)value[1];
            string typeHint = expectedType?.Name ?? "Unknown";
            typeHint = $"({typeHint})";
            if (value[0] is null)
                return "None " + typeHint;
            string name;
            if (value[0] is GameObject gameObject)
                name = gameObject.Name;
            else if (value[0] is BaseAsset asset)
            {
                bool res = AssetsRegistry.TryGetAssetPath(asset.Guid, out name);
                if (res)
                    name = Path.ChangeExtension(Path.GetRelativePath(AssetsRegistry.ContentFolderPath, name), null);
                else
                    name = "Subasset";
            }
            else
                name = value[0].GetType().Name;
            return $"{name} {typeHint}";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return Array.Empty<object>();
        }
    }
}