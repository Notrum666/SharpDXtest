using Engine;
using Engine.AssetsData;

namespace Editor.AssetsImport
{
    [AssetImporter("anim")]
    public class AnimationImporter : AssetImporter
    {
        public override int LatestVersion => 1;

        protected override void OnImportAsset(AssetImportContext importContext)
        {
            AnimationData animationData = YamlManager.LoadFromStream<AnimationData>(importContext.DataStream);

            importContext.AddMainAsset(animationData);
        }
    }
}