namespace Engine.BaseAssets.Components.Particles
{
    public class ParticleEffect_UpdateEnergy : ParticleEffect
    {
        public ParticleEffect_UpdateEnergy()
        {
            EffectShader = Shader.Create("particles_update_energy");
        }

        public override void Use(ParticleSystem system)
        {
            EffectShader.Use();
        }
    }
}