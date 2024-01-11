using Engine;
using Engine.AssetsData;

namespace Editor.AssetsImport
{
    [AssetImporter("scene")]
    public class SceneImporter : AssetImporter
    {
        protected override void OnImportAsset(AssetImportContext importContext)
        {
            SceneData sceneData = YamlManager.LoadFromStream<SceneData>(importContext.DataStream);

            importContext.AddMainAsset(sceneData);
        }
    }
}