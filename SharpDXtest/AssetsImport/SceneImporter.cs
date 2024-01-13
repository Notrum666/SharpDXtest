using Engine;
using Engine.AssetsData;

namespace Editor.AssetsImport
{
    [AssetImporter("scene")]
    public class SceneImporter : AssetImporter
    {
        public override int LatestVersion => 1;

        protected override void OnImportAsset(AssetImportContext importContext)
        {
            SceneData sceneData = YamlManager.LoadFromStream<SceneData>(importContext.DataStream);

            importContext.AddMainAsset(sceneData);
        }
    }
}