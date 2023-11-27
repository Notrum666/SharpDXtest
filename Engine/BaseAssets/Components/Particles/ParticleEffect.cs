using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.BaseAssets.Components.Particles
{
    public abstract class ParticleEffect
    {
        public Shader EffectShader { get; protected set; }

        public virtual void Update(ParticleSystem system)
        {

        }

        public abstract void Use(ParticleSystem system);
    }
}