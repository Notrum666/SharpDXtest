using LinearAlgebra;

namespace Engine.BaseAssets.Components.Particles
{
    public class ParticleEffect_PointForce : ParticleEffect
    {
        public Vector3f Point { get; set; } = Vector3f.Zero;
        public float Force { get; set; } = 0.0f;
        public bool Global { get; set; } = false;

        public ParticleEffect_PointForce()
        {
            EffectShader = Shader.GetStaticShader("particles_force_point");
        }

        public override void Use(ParticleSystem system)
        {
            EffectShader.Use();
            if (Global == system.WorldSpaceParticles)
                EffectShader.UpdateUniform("location", Point);
            else if (Global)
                EffectShader.UpdateUniform("location", (Vector3f)system.GameObject.Transform.View.TransformPoint(Point));
            else
                EffectShader.UpdateUniform("location", (Vector3f)system.GameObject.Transform.Model.TransformPoint(Point));
            EffectShader.UpdateUniform("force", Force);
        }
    }
}