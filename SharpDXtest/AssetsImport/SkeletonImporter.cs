using Engine;
using Engine.AssetsData;

namespace Editor.AssetsImport
{
    [AssetImporter("sklt")]
    public class SkeletonImporter : AssetImporter
    {
        protected override void OnImportAsset(AssetImportContext importContext)
        {
            SkeletonData skeletonData = YamlManager.LoadFromStream<SkeletonData>(importContext.DataStream);

            importContext.AddMainAsset(skeletonData);
        }
    }
}