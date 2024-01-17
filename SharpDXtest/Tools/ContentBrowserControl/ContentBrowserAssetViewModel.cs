using System;
using System.IO;

using Editor.AssetsImport;
using Engine;

namespace Editor
{
    public class ContentBrowserAssetViewModel : ViewModelBase
    {
        private string name;
        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }
        public bool IsSubAsset
        {
            get => string.IsNullOrWhiteSpace(assetPath);
        }
        private string assetPath;
        public string AssetPath
        {
            get => assetPath;
            set
            {
                assetPath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSubAsset));
            }
        }
        private AssetMeta assetMeta = null;
        public AssetMeta AssetMeta
        {
            get => assetMeta;
            set
            {
                assetMeta = value;
                OnPropertyChanged();
            }
        }
        public ContentBrowserAssetViewModel()
        {

        }
        public ContentBrowserAssetViewModel(string pathToMeta)
        {
            AssetPath = pathToMeta;
            Name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(pathToMeta.TrimEnd(Path.DirectorySeparatorChar)));
            try
            {
                AssetMeta = YamlManager.LoadFromFile<AssetMeta>(pathToMeta);
            }
            catch
            {
                Logger.Log(LogType.Warning, "Unable to load asset meta for " + AssetsRegistry.ContentFolderName + "\\" +
                    Path.GetRelativePath(AssetsRegistry.ContentFolderPath, pathToMeta));
            }
        }
    }
}
