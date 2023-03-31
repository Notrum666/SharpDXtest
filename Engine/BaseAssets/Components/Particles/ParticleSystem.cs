using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;

using LinearAlgebra;

using Buffer = SharpDX.Direct3D11.Buffer;
using System.Runtime.InteropServices;
using SharpDX;
using Engine.BaseAssets.Components.Particles;
using System.Drawing.Drawing2D;
using SharpDX.Multimedia;
using System.Runtime.Remoting.Messaging;
using SharpDX.Direct3D9;
using System.Runtime.CompilerServices;

namespace Engine.BaseAssets.Components
{
    public sealed class ParticleSystem : Component
    {
        public struct Particle
        {
            public Vector3f position;
            public float energy;
            public Vector3f velocity;
        }

        private Material material = new Material();
        public Material Material
        {
            get
            {
                return material;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Material", "Material can't be null.");
                material = value;
            }
        }
        private Vector2f size = new Vector2f(1.0f, 1.0f);
        public Vector2f Size
        {
            get
            {
                return size;
            }
            set
            {
                size = value;
            }
        }
        private int maxParticles = 1024;
        public int MaxParticles
        {
            get
            {
                return maxParticles;
            }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("MaxParticles", "Value must be positive.");
                value--;
                value |= value >> 1;
                value |= value >> 2;
                value |= value >> 4;
                value |= value >> 8;
                value |= value >> 16;
                maxParticles = value + 1;
            }
        }
        public int CurParticles { get; private set; } = 0;
        public bool WorldSpaceParticles { get; set; } = false;
        public List<ParticleEffect> ParticleEffects { get; private set; } = new List<ParticleEffect>();
        private ParticleEffect_UpdateEnergy energyUpdater;

        private Buffer particlesPool;
        private UnorderedAccessView particlesPoolView;
        private ShaderResourceView particlesPoolResourceView;
        private Buffer counterRetrieveBuffer;
        public ParticleSystem()
        {
            counterRetrieveBuffer = new Buffer(GraphicsCore.CurrentDevice, sizeof(uint), ResourceUsage.Staging, BindFlags.None, CpuAccessFlags.Read, ResourceOptionFlags.None, 0);
        }
        public void Render()
        {
            GraphicsCore.CurrentDevice.ImmediateContext.VertexShader.SetShaderResource(0, particlesPoolResourceView);
            GraphicsCore.CurrentDevice.ImmediateContext.Draw(CurParticles, 0);
        }
        private int getParticlesAmount()
        {
            GraphicsCore.CurrentDevice.ImmediateContext.CopyStructureCount(counterRetrieveBuffer, 0, particlesPoolView);
            DataStream stream;
            GraphicsCore.CurrentDevice.ImmediateContext.MapSubresource(counterRetrieveBuffer, MapMode.Read, MapFlags.None, out stream);
            uint amount = stream.Read<uint>();
            GraphicsCore.CurrentDevice.ImmediateContext.UnmapSubresource(counterRetrieveBuffer, 0);
            return (int)amount;
        }
        protected override void Initialized()
        {
            int particleStructureSize = Marshal.SizeOf(typeof(Particle));
            particlesPool = new Buffer(GraphicsCore.CurrentDevice, particleStructureSize * MaxParticles, ResourceUsage.Default, BindFlags.UnorderedAccess | BindFlags.ShaderResource, CpuAccessFlags.None, ResourceOptionFlags.BufferStructured, particleStructureSize);

            particlesPoolView = new UnorderedAccessView(GraphicsCore.CurrentDevice, particlesPool, new UnorderedAccessViewDescription()
            {
                Dimension = UnorderedAccessViewDimension.Buffer,
                Format = SharpDX.DXGI.Format.Unknown,
                Buffer = new UnorderedAccessViewDescription.BufferResource()
                {
                    ElementCount = maxParticles,
                    FirstElement = 0,
                    Flags = UnorderedAccessViewBufferFlags.Counter
                }
            });

            particlesPoolResourceView = new ShaderResourceView(GraphicsCore.CurrentDevice, particlesPool, new ShaderResourceViewDescription()
            {
                Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Buffer,
                Format = SharpDX.DXGI.Format.Unknown,
                Buffer = new ShaderResourceViewDescription.BufferResource()
                {
                    FirstElement = 0,
                    ElementCount = maxParticles
                }
            });

            Shader initShader = AssetsManager.Shaders["particles_init"];

            initShader.use();
            initShader.updateUniform("maxParticles", maxParticles);
            initShader.uploadUpdatedUniforms();

            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, particlesPoolView);

            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch((int)Math.Ceiling(maxParticles / 64.0), 1, 1);

            energyUpdater = new ParticleEffect_UpdateEnergy();
        }
        public override void Update()
        {
            foreach (ParticleEffect effect in ParticleEffects)
                effect.Update(this);

            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, particlesPoolView);

            applyEffect(energyUpdater);

            foreach (ParticleEffect effect in ParticleEffects)
                applyEffect(effect);

            sortParticles();

            CurParticles = getParticlesAmount();

            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, null);
        }
        private void sortParticles()
        {
            Shader sortShader = AssetsManager.Shaders["particles_bitonic_sort_step"];
            sortShader.use();
            sortShader.updateUniform("maxParticles", maxParticles);
            for (int subArraySize = 2; subArraySize >> 1 < maxParticles; subArraySize <<= 1)
            {
                for (int compareDistance = subArraySize >> 1; compareDistance > 0; compareDistance >>= 1)
                {
                    sortShader.updateUniform("subArraySize", subArraySize);
                    sortShader.updateUniform("compareDist", compareDistance);
                    sortShader.uploadUpdatedUniforms();
                    GraphicsCore.CurrentDevice.ImmediateContext.Dispatch((int)Math.Ceiling(maxParticles / 64.0), 1, 1);
                }
            }
        }
        private void applyEffect(ParticleEffect effect)
        {
            effect.Use(this);
            effect.EffectShader.updateUniform("deltaTime", (float)Time.DeltaTime);
            effect.EffectShader.updateUniform("maxParticles", maxParticles);
            effect.EffectShader.uploadUpdatedUniforms();
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch((int)Math.Ceiling(maxParticles / 64.0), 1, 1);
        }
    }
}
