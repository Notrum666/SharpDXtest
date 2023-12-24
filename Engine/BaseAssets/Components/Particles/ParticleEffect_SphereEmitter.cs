using System;

using LinearAlgebra;

namespace Engine.BaseAssets.Components.Particles
{
    public class ParticleEffect_SphereEmitter : ParticleEffect
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
        private float radius = 1;
        public float Radius
        {
            get => radius;
            set
            {
                if (value < 0.0f || innerRadius > value)
                    throw new ArgumentOutOfRangeException(nameof(Radius), "Radius can't be negative or less than inner radius.");
                radius = value;
            }
        }
        private float innerRadius = 0;
        public float InnerRadius
        {
            get => innerRadius;
            set
            {
                if (value < 0.0f || value > Radius)
                    throw new ArgumentOutOfRangeException(nameof(InnerRadius), "Inner radius can't be negative or greater than radius.");
                innerRadius = value;
            }
        }
        public bool Global { get; set; } = false;
        private double toEmitAccumulator = 0.0;

        public ParticleEffect_SphereEmitter()
        {
            EffectShader = Shader.LoadStaticShader("particles_emit_sphere");
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
            EffectShader.updateUniform("radius", radius);
            EffectShader.updateUniform("innerRadius", innerRadius);
        }
    }
}