namespace Engine.BaseAssets.Components.Particles
{
    public class ParticleEffect_UpdatePhysics : ParticleEffect
    {
        public ParticleEffect_UpdatePhysics()
        {
            EffectShader = Shader.GetStaticShader("particles_update_physics");
        }

        public override void Use(ParticleSystem system)
        {
            EffectShader.Use();
        }
    }
}