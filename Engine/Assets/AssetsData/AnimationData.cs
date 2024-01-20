using System;
using System.Collections.Generic;
using System.IO;

namespace Engine.AssetsData
{
    [AssetData<SkeletalAnimation>]
    public class AnimationData : NativeAssetData
    {
        public sealed override string FileExtension => ".anim";

        public string Name;
        public float DurationInTicks;
        public float TickPerSecond;
        public List<AnimationChannel> Channels = new List<AnimationChannel>();

        protected sealed override void SetDefaultValues()
        {
            throw new NotImplementedException();
        }
        
        public sealed override void Serialize(BinaryWriter writer)
        {
            YamlManager.SaveToStream(writer.BaseStream, this);
        }

        public sealed override void Deserialize(BinaryReader reader)
        {
            YamlManager.LoadFromStream(reader.BaseStream, this);
        }

        public override SkeletalAnimation ToRealAsset(BaseAsset targetAsset = null)
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