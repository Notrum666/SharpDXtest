using System;
using System.IO;

using Editor.AssetsImport;
using Engine;
using Engine.AssetsData;

namespace Editor
{
    public class ContentBrowserAssetViewModel : ViewModelBase
    {
        private string name;
        public string Name => name;
        public bool IsSubAsset => string.IsNullOrWhiteSpace(assetPath);
        private string assetPath;
        public string AssetPath => assetPath;
        private Guid assetGuid = Guid.Empty;
        public Guid AssetGuid => assetGuid;
        private AssetMeta assetMeta = null;
        public AssetMeta AssetMeta => assetMeta;
        private Type associatedAssetDataType = null;
        public Type AssociatedAssetDataType => associatedAssetDataType;
        public void Open()
        {
            if (associatedAssetDataType is null)
                return;

            if (associatedAssetDataType.IsSameOrSubclassOf(typeof(SceneData)))
                SceneManager.LoadSceneByPath(Path.ChangeExtension(Path.GetRelativePath(AssetsRegistry.ContentFolderPath, assetPath), null));
        }
        public ContentBrowserAssetViewModel(string name, Type dataType, Guid assetGuid)
        {
            this.name = name;
            associatedAssetDataType = dataType;
            this.assetGuid = assetGuid;
        }
        public ContentBrowserAssetViewModel(string pathToMeta)
        {
            assetPath = pathToMeta;
            name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(pathToMeta.TrimEnd(Path.DirectorySeparatorChar)));
            try
            {
                assetMeta = YamlManager.LoadFromFile<AssetMeta>(pathToMeta);
                assetGuid = assetMeta.Guid;
            }
            catch
            {
                Logger.Log(LogType.Warning, "Unable to load asset meta for " + AssetsRegistry.ContentFolderName + "\\" +
                    Path.GetRelativePath(AssetsRegistry.ContentFolderPath, pathToMeta));
            }
            if (!AssetsManager.TryGetAssetDataTypeByGuid(assetMeta.Guid, out associatedAssetDataType))
                Logger.Log(LogType.Warning, $"Unable to resolve asset type of asset {Name}");
        }
    }
}
