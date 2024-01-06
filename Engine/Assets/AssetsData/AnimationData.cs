using System.Collections.Generic;
using System.IO;

namespace Engine.AssetsData
{
    [AssetData<SkeletalAnimation>]
    public class AnimationData : AssetData
    {
        public string Name;
        public float DurationInTicks;
        public float TickPerSecond;
        public List<AnimationChannel> Channels = new List<AnimationChannel>();

        public override void Serialize(BinaryWriter writer)
        {
            YamlManager.SaveToStream(writer.BaseStream, this);
        }

        public override void Deserialize(BinaryReader reader)
        {
            YamlManager.LoadFromStream(reader.BaseStream, this);
        }

        public override SkeletalAnimation ToRealAsset()
        {
            SkeletalAnimation animation = new SkeletalAnimation();

            animation.Name = Name;
            animation.DurationInTicks = DurationInTicks;
            animation.TickPerSecond = TickPerSecond;
            animation.Channels = Channels;

            return animation;
        }
    }
}
