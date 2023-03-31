using LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.BaseAssets.Components.Particles
{
    internal class ParticleEffect_ConstantForce : ParticleEffect
    {
        public Vector3f Force { get; set; } = Vector3f.Zero;
        public bool Global { get; set; } = false;
        public ParticleEffect_ConstantForce()
        {
            EffectShader = AssetsManager.Shaders["particles_force_constant"];
        }
        public override void Use(ParticleSystem system)
        {
            EffectShader.use();
            if (Global == system.WorldSpaceParticles)
                EffectShader.updateUniform("force", Force);
            else if (Global)
                EffectShader.updateUniform("force", (Vector3f)system.GameObject.Transform.View.TransformDirection(Force));
            else
                EffectShader.updateUniform("force", (Vector3f)system.GameObject.Transform.Model.TransformDirection(Force));
        }
    }
}
