using Engine;
using Engine.AssetsData;

namespace Editor.AssetsImport
{
    [AssetImporter("sklt")]
    public class SkeletonImporter : AssetImporter
    {
        public override int LatestVersion => 1;

        protected override void OnImportAsset(AssetImportContext importContext)
        {
            SkeletonData skeletonData = YamlManager.LoadFromStream<SkeletonData>(importContext.DataStream);

            importContext.AddMainAsset(skeletonData);
        }
    }
}