using LinearAlgebra;
using System.Collections.Generic;
using System.IO;

namespace Engine.AssetsData
{
    [AssetData<Animation>]
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

        public override Animation ToRealAsset()
        {
            Animation animation = new Animation();

            animation.Name = Name;
            animation.DurationInTicks = DurationInTicks;
            animation.TickPerSecond = TickPerSecond;
            animation.Channels = Channels;

            return animation;
        }
    }

    public class AnimationChannel
    {
        public string BoneName;

        public class ScalingKey
        {
            public float Time;
            public Vector3f Scaling;
        }
        public List<ScalingKey> ScalingKeys = new List<ScalingKey>();

        public class RotationKey
        {
            public float Time;
            public Quaternion Rotation;
        }
        public List<RotationKey> RotationKeys = new List<RotationKey>();

        public class PositionKey
        {
            public float Time;
            public Vector3f Position;
        }
        public List<PositionKey> PositionKeys = new List<PositionKey>();
    }
}
