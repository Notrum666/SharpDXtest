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
            EffectShader = AssetsManager_Old.Shaders["particles_force_point"];
        }

        public override void Use(ParticleSystem system)
        {
            EffectShader.use();
            if (Global == system.WorldSpaceParticles)
                EffectShader.updateUniform("location", Point);
            else if (Global)
                EffectShader.updateUniform("location", (Vector3f)system.GameObject.Transform.View.TransformPoint(Point));
            else
                EffectShader.updateUniform("location", (Vector3f)system.GameObject.Transform.Model.TransformPoint(Point));
            EffectShader.updateUniform("force", Force);
        }
    }
}