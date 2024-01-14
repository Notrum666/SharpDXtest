using LinearAlgebra;

namespace Engine.BaseAssets.Components.Particles
{
    public class ParticleEffect_ConstantForce : ParticleEffect
    {
        public Vector3f Force { get; set; } = Vector3f.Zero;
        public bool Global { get; set; } = false;

        public ParticleEffect_ConstantForce()
        {
            EffectShader = Shader.Create(@"BaseAssets\Shaders\Particles\particles_force_constant.csh");
        }

        public override void Use(ParticleSystem system)
        {
            EffectShader.Use();
            if (Global == system.WorldSpaceParticles)
                EffectShader.UpdateUniform("force", Force);
            else if (Global)
                EffectShader.UpdateUniform("force", (Vector3f)system.GameObject.Transform.View.TransformDirection(Force));
            else
                EffectShader.UpdateUniform("force", (Vector3f)system.GameObject.Transform.Model.TransformDirection(Force));
        }
    }
}