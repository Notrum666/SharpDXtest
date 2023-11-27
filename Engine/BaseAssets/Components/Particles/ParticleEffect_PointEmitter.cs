using System;
using LinearAlgebra;

namespace Engine.BaseAssets.Components.Particles
{
    public class ParticleEffect_PointEmitter : ParticleEffect
    {
        public Vector3f Point { get; set; } = Vector3f.Zero;
        private int rate = 20;
        public int Rate
        {
            get => rate;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Rate), "Particles per second can't be negative.");
                rate = value;
            }
        }
        public bool Global { get; set; } = false;
        private double toEmitAccumulator = 0.0;

        public ParticleEffect_PointEmitter()
        {
            EffectShader = AssetsManager.Shaders["particles_emit_point"];
        }

        public override void Update(ParticleSystem system)
        {
            toEmitAccumulator += rate * Time.DeltaTime;
        }

        public override void Use(ParticleSystem system)
        {
            int toEmit = (int)Math.Floor(toEmitAccumulator);
            toEmitAccumulator -= toEmit;
            EffectShader.use();
            if (Global == system.WorldSpaceParticles)
                EffectShader.updateUniform("location", Point);
            else if (Global)
                EffectShader.updateUniform("location", (Vector3f)system.GameObject.Transform.View.TransformPoint(Point));
            else
                EffectShader.updateUniform("location", (Vector3f)system.GameObject.Transform.Model.TransformPoint(Point));
            EffectShader.updateUniform("toEmit", toEmit);
        }
    }
}