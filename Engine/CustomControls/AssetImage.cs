using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;

using YamlDotNet.Core.Tokens;

namespace Engine
{
    public class AssetImage : Image
    {
        public static readonly DependencyProperty SourcePathProperty = DependencyProperty.Register("SourcePath", typeof(string), typeof(AssetImage),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender, OnSourcePathPropertyChanged));
        public string SourcePath
        {
            get => (string)GetValue(SourcePathProperty);
            set => SetValue(SourcePathProperty, value);
        }
        private static void OnSourcePathPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AssetImage assetImage = (AssetImage)sender;
            string path = (string)e.NewValue;

            if (DesignerProperties.GetIsInDesignMode(assetImage))
            {
                string contentPath = Path.Combine(Environment.CurrentDirectory, "Content");
                if (!Directory.Exists(contentPath))
                    throw new Exception("'Content' directory was not found, maybe content folder name was changed, please notify the developers");
                assetImage.Source = new BitmapImage(new Uri(Path.Combine(contentPath, path), UriKind.Absolute));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(path))
                    return;
                Texture texture = AssetsManager.LoadAssetAtPath<Texture>(path);
                if (texture is null)
                {
                    string name = assetImage.Name is not null ? $" {assetImage.Name}" : "";
                    Logger.Log(LogType.Error, $"Image {path} is not found{name}");
                    return;
                }
                assetImage.Source = texture.Source;
            }
        }
        public AssetImage()
        {
            Loaded += AssetImage_Loaded;
        }

        private void AssetImage_Loaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            string path = SourcePath;
            if (string.IsNullOrWhiteSpace(path))
                return;
            Texture texture = AssetsManager.LoadAssetAtPath<Texture>(path);
            if (texture is null)
            {
                string name = Name is not null ? $" {Name}" : "";
                Logger.Log(LogType.Error, $"Image {path} is not found{name}");
                return;
            }
            Source = texture.Source;
        }
    }
}
