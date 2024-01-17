using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

using Engine.Assets;

namespace Editor
{
    [ValueConversion(typeof(Guid), typeof(string))]
    public sealed class GuidToStringConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value[1] is null)
                return "Guid arrays are not supported yet.";
            Guid guid = (Guid)value[0];
            Type expectedType = ((FieldInfo)value[1]).GetCustomAttribute<GuidExpectedTypeAttribute>()?.ExpectedType;
            string typeHint = expectedType?.Name ?? "Unknown";
            typeHint = $"({typeHint})";
            if (guid == Guid.Empty)
                return $"None {typeHint}";
            if (!AssetsRegistry.TryGetAssetPath(guid, out string path))
                return $"Subasset {typeHint}";
            return $"{Path.ChangeExtension(Path.GetRelativePath(AssetsRegistry.ContentFolderPath, path), null)} {typeHint}";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return Array.Empty<object>();
        }
    }
}