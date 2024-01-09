using System;

using LinearAlgebra;

namespace Engine.BaseAssets.Components.Particles
{
    public class ParticleEffect_PointEmitter : ParticleEffect
    {
        public Vector3f Point { get; set; } = Vector3f.Zero;
        private int rate = 20;
        public Ranged<int> Rate => new Ranged<int>(ref rate, 0);

        public bool Global { get; set; } = false;
        private double toEmitAccumulator = 0.0;

        public ParticleEffect_PointEmitter()
        {
            EffectShader = Shader.Create("particles_emit_point");
        }

        public override void Update(ParticleSystem system)
        {
            toEmitAccumulator += rate * Time.DeltaTime;
        }

        public override void Use(ParticleSystem system)
        {
            int toEmit = (int)Math.Floor(toEmitAccumulator);
            toEmitAccumulator -= toEmit;
            EffectShader.Use();
            if (Global == system.WorldSpaceParticles)
                EffectShader.UpdateUniform("location", Point);
            else if (Global)
                EffectShader.UpdateUniform("location", (Vector3f)system.GameObject.Transform.View.TransformPoint(Point));
            else
                EffectShader.UpdateUniform("location", (Vector3f)system.GameObject.Transform.Model.TransformPoint(Point));
            EffectShader.UpdateUniform("toEmit", toEmit);
        }
    }
}