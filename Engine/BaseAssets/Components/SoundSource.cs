using LinearAlgebra;
using SharpDX.X3DAudio;

namespace Engine.BaseAssets.Components
{
    public sealed class SoundSource : Component
    {
        public Emitter Source { get; private set; }

        public SoundSource()
        {
            Source = new Emitter();
            Source.ChannelCount = 1;
            Source.CurveDistanceScaler = float.MinValue;
        }

        public void play(Sound sound)
        {
            SoundCore.PlayFrom(sound, this);
        }

        public override void Update()
        {
            Vector3 pos = GameObject.Transform.Position;
            Source.Position.X = -(float)pos.x;
            Source.Position.Y = (float)pos.y;
            Source.Position.Z = (float)pos.z;
            Vector3 forward = GameObject.Transform.Forward;
            Source.OrientFront.X = -(float)forward.x;
            Source.OrientFront.Y = (float)forward.y;
            Source.OrientFront.Z = (float)forward.z;
            Vector3 up = GameObject.Transform.Up;
            Source.OrientTop.X = -(float)up.x;
            Source.OrientTop.Y = (float)up.y;
            Source.OrientTop.Z = (float)up.z;
            Rigidbody rb = GameObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 velocity = rb.Velocity;
                Source.Velocity.X = -(float)velocity.x;
                Source.Velocity.Y = (float)velocity.y;
                Source.Velocity.Z = (float)velocity.z;
            }
        }
    }
}