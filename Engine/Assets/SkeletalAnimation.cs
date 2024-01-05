using LinearAlgebra;
using System.Collections.Generic;

namespace Engine
{
    public class SkeletalAnimation : BaseAsset
    {
        public string Name;
        public float DurationInTicks;
        public float TickPerSecond;
        public List<AnimationChannel> Channels = new List<AnimationChannel>();
    }

    // TODO: split to two classes for Animation and AnimationData
    public class AnimationChannel
    {
        public string BoneName;
        public List<ScalingKey> ScalingKeys = new List<ScalingKey>();
        public List<RotationKey> RotationKeys = new List<RotationKey>();
        public List<PositionKey> PositionKeys = new List<PositionKey>();

        public struct ScalingKey
        {
            public float Time;
            public Vector3f Scaling;
        }
        public struct RotationKey
        {
            public float Time;
            public Quaternion Rotation;
        }
        public struct PositionKey
        {
            public float Time;
            public Vector3f Position;
        }
    }
}
