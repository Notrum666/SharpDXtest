using Engine;
using Engine.AssetsData;

namespace Editor.AssetsImport
{
    [AssetImporter("mat")]
    public class MaterialImporter : AssetImporter
    {
        public override int LatestVersion => 1;

        protected override void OnImportAsset(AssetImportContext importContext)
        {
            AssetsManager.SaveNativeAssetData<MaterialData>(importContext.AssetContentPath, importContext.MainGuid, importContext.DataStream);
        }
    }
}