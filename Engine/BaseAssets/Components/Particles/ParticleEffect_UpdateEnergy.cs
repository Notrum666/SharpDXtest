using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.BaseAssets.Components.Particles
{
    public class ParticleEffect_UpdateEnergy : ParticleEffect
    {
        public ParticleEffect_UpdateEnergy()
        {
            EffectShader = AssetsManager.Shaders["particles_update_energy"];
        }

        public override void Use(ParticleSystem system)
        {
            EffectShader.use();
        }
    }
}