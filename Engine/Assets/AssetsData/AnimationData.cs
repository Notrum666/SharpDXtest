using System;
using System.Collections.Generic;

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