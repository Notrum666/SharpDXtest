using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

using Engine.AssetsData;

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
            Type t = value as Type;
            string path;
            if (t == typeof(ModelData))
                path = "Icons\\ModelIcon.png";
            else if (t == typeof(SkeletonData))
                path = "Icons\\SkeletonIcon.png";
            else if (t == typeof(AnimationData))
                path = "Icons\\SkeletalAnimationIcon.png";
            else if (t == typeof(TextureData))
                path = "Icons\\TextureIcon.png";
            else if (t == typeof(SceneData))
                path = "Icons\\SceneIcon.png";
            else if (t == typeof(ScriptData))
                path = "Icons\\ScriptIcon.png";
            else if (t == typeof(SoundData))
                path = "Icons\\SoundIcon.png";
            else if (t == typeof(MaterialData))
                path = "Icons\\MaterialIcon.png";
            else
                path = "Icons\\FileIcon.png";

            return new Uri(path, UriKind.Relative);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}