namespace Engine.BaseAssets.Components.Particles
{
    public class ParticleEffect_UpdatePhysics : ParticleEffect
    {
        public ParticleEffect_UpdatePhysics()
        {
            EffectShader = Shader.Create(@"BaseAssets\Shaders\Particles\particles_update_physics.csh");
        }

        public override void Use(ParticleSystem system)
        {
            EffectShader.Use();
        }
    }
}