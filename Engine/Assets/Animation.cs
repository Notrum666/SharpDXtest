using LinearAlgebra;
using System.Collections.Generic;

namespace Engine
{
    public class Animation : BaseAsset
    {
        public string Name;
        public float DurationInTicks;
        public float TickPerSecond;
        public List<AnimationChannel> Channels = new List<AnimationChannel>();
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
