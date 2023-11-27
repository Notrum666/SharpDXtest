using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.XAudio2;
using SharpDX.X3DAudio;
using SharpDX.Multimedia;
using SharpDX.XAPO;
using SharpDX.XAPO.Fx;
using LinearAlgebra;

namespace Engine.BaseAssets.Components
{
    public sealed class SoundListener : Component
    {
        public Listener Listener { get; private set; }
        public bool IsCurrent
        {
            get => SoundCore.CurrentListener == this;
            set
            {
                if (value)
                {
                    makeCurrent();
                }
                else
                {
                    if (IsCurrent)
                        SoundCore.CurrentListener = this;
                }
            }
        }

        public SoundListener()
        {
            Listener = new Listener();
        }

        public override void Update()
        {
            Vector3 pos = GameObject.Transform.Position;
            Listener.Position.X = -(float)pos.x;
            Listener.Position.Y = (float)pos.y;
            Listener.Position.Z = (float)pos.z;
            Vector3 forward = GameObject.Transform.Forward;
            Listener.OrientFront.X = -(float)forward.x;
            Listener.OrientFront.Y = (float)forward.y;
            Listener.OrientFront.Z = (float)forward.z;
            Vector3 up = GameObject.Transform.Up;
            Listener.OrientTop.X = -(float)up.x;
            Listener.OrientTop.Y = (float)up.y;
            Listener.OrientTop.Z = (float)up.z;
            Rigidbody rb = GameObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 velocity = rb.Velocity;
                Listener.Velocity.X = -(float)velocity.x;
                Listener.Velocity.Y = (float)velocity.y;
                Listener.Velocity.Z = (float)velocity.z;
            }
        }

        public void makeCurrent()
        {
            SoundCore.CurrentListener = this;
        }
    }
}