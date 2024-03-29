﻿using LinearAlgebra;

using SharpDX.X3DAudio;

namespace Engine.BaseAssets.Components
{
    public sealed class SoundListener : BehaviourComponent
    {
        public Listener Listener { get; private set; }
        public bool IsCurrent
        {
            get => SoundCore.CurrentListener == this;
            set
            {
                if (value)
                    MakeCurrent();
                else
                {
                    if (IsCurrent)
                        SoundCore.CurrentListener = null;
                }
            }
        }

        protected override void OnInitialized()
        {
            Listener = new Listener();

            if (SoundCore.CurrentListener == null)
                IsCurrent = true;
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

        public void MakeCurrent()
        {
            SoundCore.CurrentListener = this;
        }
    }
}