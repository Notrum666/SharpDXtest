using System;
using System.IO;

using Editor.AssetsImport;
using Engine;
using Engine.AssetsData;

namespace Editor
{
    public class InspectorAssetViewModel : ViewModelBase
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
            get => string.IsNullOrWhiteSpace(filePath);
        }
        private string filePath;
        public string FilePath
        {
            get => filePath;
            set
            {
                filePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSubAsset));
            }
        }
        private object fileObject;
        private object targetObject;
        public object TargetObject
        {
            get => targetObject;
            private set
            {
                targetObject = value;
                OnPropertyChanged();
            }
        }
        public InspectorAssetViewModel(ContentBrowserAssetViewModel assetViewModel)
        {
            Name = assetViewModel.Name;

            string assetExtension = Path.GetExtension(Path.GetFileNameWithoutExtension(assetViewModel.AssetPath));
            if (assetExtension == ".scene" || assetExtension == ".mat") // native asset
            {
                FilePath = Path.ChangeExtension(assetViewModel.AssetPath, null);
                TargetObject = AssetsRegistry.LoadNativeAsset(FilePath);
                fileObject = TargetObject;
            }
            else // normal asset
            {
                FilePath = assetViewModel.AssetPath;
                fileObject = assetViewModel.AssetMeta;
                TargetObject = assetViewModel.AssetMeta.ImportSettings;
            }
        }
        public void Save()
        {
            string assetPath = filePath;
            if (Path.GetExtension(assetPath) == AssetMeta.MetaExtension)
                assetPath = Path.ChangeExtension(assetPath, null);

            YamlManager.SaveObjectToFile(filePath, fileObject);
            AssetsRegistry.ImportAsset(assetPath);
        }
    }
}
