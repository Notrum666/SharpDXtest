namespace Engine.BaseAssets.Components.Particles
{
    public class ParticleEffect_UpdateEnergy : ParticleEffect
    {
        public ParticleEffect_UpdateEnergy()
        {
            EffectShader = Shader.Create(@"BaseAssets\Shaders\Particles\particles_update_energy.csh");
        }

        public override void Use(ParticleSystem system)
        {
            EffectShader.Use();
        }
    }
}