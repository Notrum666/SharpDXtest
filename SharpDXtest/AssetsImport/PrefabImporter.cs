using Engine;
using Engine.AssetsData;

namespace Editor.AssetsImport
{
    [AssetImporter("scene")]
    public class PrefabImporter : AssetImporter
    {
        public override int LatestVersion => 1;

        protected override void OnImportAsset(AssetImportContext importContext)
        {
            AssetsManager.SaveNativeAssetData<PrefabData>(importContext.AssetContentPath, importContext.MainGuid, importContext.DataStream);
        }
    }
}