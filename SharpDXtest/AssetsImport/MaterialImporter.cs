using Engine;
using Engine.AssetsData;

namespace Editor.AssetsImport
{
    [AssetImporter("mat")]
    public class MaterialImporter : AssetImporter
    {
        protected override void OnImportAsset(AssetImportContext importContext)
        {
            MaterialData materialData = YamlManager.LoadFromStream<MaterialData>(importContext.DataStream);

            importContext.AddMainAsset(materialData);
        }
    }
}