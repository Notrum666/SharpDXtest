using Engine.AssetsData;
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
}
