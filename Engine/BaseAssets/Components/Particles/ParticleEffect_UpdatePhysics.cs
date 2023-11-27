using LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.BaseAssets.Components.Particles
{
    public class ParticleEffect_UpdatePhysics : ParticleEffect
    {
        public ParticleEffect_UpdatePhysics()
        {
            EffectShader = AssetsManager.Shaders["particles_update_physics"];
        }

        public override void Use(ParticleSystem system)
        {
            EffectShader.use();
        }
    }
}