using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

using Engine.BaseAssets.Components.Particles;

using LinearAlgebra;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

using Buffer = SharpDX.Direct3D11.Buffer;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace Engine.BaseAssets.Components
{
    public sealed class ParticleSystem : BehaviourComponent
    {
        [SerializedField]
        private Material material = Material.Default;
        [SerializedField]
        private Vector2f size = new Vector2f(1.0f, 1.0f);
        [SerializedField]
        private int maxParticles = 1024;
        [SerializedField]
        private bool worldSpaceParticles;

        public Material Material
        {
            get => material;
            set
            {
                if (value == null)
                {
                    Logger.Log(LogType.Error, "Material of ParticleSystem can't be null");
                    return;
                }
                material = value;
            }
        }

        public Vector2f Size
        {
            get => size;
            set => size = value;
        }

        public int MaxParticles
        {
            get => maxParticles;
            set
            {
                if (value <= 0)
                {
                    Logger.Log(LogType.Error, "MaxParticles must be positive");
                    return;
                }
                maxParticles = RoundUpToPowerOfTwo(value);
            }
        }

        public bool WorldSpaceParticles { get => worldSpaceParticles; set => worldSpaceParticles = value; }

        public override void OnFieldChanged(FieldInfo fieldInfo)
        {
            base.OnFieldChanged(fieldInfo);

            switch (fieldInfo.Name)
            {
                case nameof(maxParticles):
                    MaxParticles = maxParticles;
                    return;
            }
        }

        public List<ParticleEffect> ParticleEffects { get; } = new List<ParticleEffect>();
        public int CurParticles { get; private set; } = 0;

        private ParticleEffect_UpdateEnergy energyUpdater;

        private Buffer particlesPool;
        private UnorderedAccessView particlesPoolView;
        private ShaderResourceView particlesPoolResourceView;
        private Buffer rngPool;
        private UnorderedAccessView rngPoolView;
        private Buffer counterRetrieveBuffer;

        private int kernelsCount = 0;

        protected override void OnInitialized()
        {
            counterRetrieveBuffer = new Buffer(GraphicsCore.CurrentDevice, sizeof(uint), ResourceUsage.Staging, BindFlags.None, CpuAccessFlags.Read, ResourceOptionFlags.None, 0); // In case of error - move back to constructor

            kernelsCount = (int)Math.Ceiling(maxParticles / 64.0);

            int particleStructureSize = Marshal.SizeOf(typeof(Particle));
            particlesPool = new Buffer(GraphicsCore.CurrentDevice, particleStructureSize * MaxParticles, ResourceUsage.Default, BindFlags.UnorderedAccess | BindFlags.ShaderResource, CpuAccessFlags.None, ResourceOptionFlags.BufferStructured, particleStructureSize);

            particlesPoolView = new UnorderedAccessView(GraphicsCore.CurrentDevice, particlesPool, new UnorderedAccessViewDescription()
            {
                Dimension = UnorderedAccessViewDimension.Buffer,
                Format = Format.Unknown,
                Buffer = new UnorderedAccessViewDescription.BufferResource()
                {
                    ElementCount = maxParticles,
                    FirstElement = 0,
                    Flags = UnorderedAccessViewBufferFlags.Counter
                }
            });

            particlesPoolResourceView = new ShaderResourceView(GraphicsCore.CurrentDevice, particlesPool, new ShaderResourceViewDescription()
            {
                Dimension = ShaderResourceViewDimension.Buffer,
                Format = Format.Unknown,
                Buffer = new ShaderResourceViewDescription.BufferResource()
                {
                    FirstElement = 0,
                    ElementCount = maxParticles
                }
            });

            Random rng = new Random();
            DataStream rngStream = new DataStream(kernelsCount * sizeof(int), true, true);
            byte[] rngData = new byte[kernelsCount * sizeof(int)];
            rng.NextBytes(rngData);
            rngStream.Write(rngData, 0, kernelsCount * sizeof(int));
            rngStream.Position = 0;
            rngPool = new Buffer(GraphicsCore.CurrentDevice, rngStream, kernelsCount * sizeof(int), ResourceUsage.Default, BindFlags.UnorderedAccess, CpuAccessFlags.None, ResourceOptionFlags.BufferStructured, sizeof(int));

            rngPoolView = new UnorderedAccessView(GraphicsCore.CurrentDevice, rngPool, new UnorderedAccessViewDescription()
            {
                Dimension = UnorderedAccessViewDimension.Buffer,
                Format = Format.Unknown,
                Buffer = new UnorderedAccessViewDescription.BufferResource()
                {
                    ElementCount = kernelsCount,
                    FirstElement = 0,
                    Flags = UnorderedAccessViewBufferFlags.None
                }
            });

            Shader initShader = Shader.Create("particles_init");

            initShader.Use();
            initShader.UpdateUniform("maxParticles", maxParticles);
            initShader.UploadUpdatedUniforms();

            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, particlesPoolView);

            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(kernelsCount, 1, 1);

            energyUpdater = new ParticleEffect_UpdateEnergy();
        }

        public override void Update()
        {
            foreach (ParticleEffect effect in ParticleEffects)
                effect.Update(this);

            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, particlesPoolView);
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(1, rngPoolView);

            ApplyEffect(energyUpdater);

            foreach (ParticleEffect effect in ParticleEffects)
                ApplyEffect(effect);

            SortParticles();

            CurParticles = GetParticlesAmount();

            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, null);
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(1, null);
        }

        public void Render()
        {
            GraphicsCore.CurrentDevice.ImmediateContext.VertexShader.SetShaderResource(0, particlesPoolResourceView);
            GraphicsCore.CurrentDevice.ImmediateContext.Draw(CurParticles, 0);
        }

        private int GetParticlesAmount()
        {
            GraphicsCore.CurrentDevice.ImmediateContext.CopyStructureCount(counterRetrieveBuffer, 0, particlesPoolView);
            DataStream stream;
            GraphicsCore.CurrentDevice.ImmediateContext.MapSubresource(counterRetrieveBuffer, MapMode.Read, MapFlags.None, out stream);
            uint amount = stream.Read<uint>();
            GraphicsCore.CurrentDevice.ImmediateContext.UnmapSubresource(counterRetrieveBuffer, 0);
            return (int)amount;
        }

        private void SortParticles()
        {
            // TODO: definitely a bad idea
            Shader sortShader = Shader.Create(@"BaseAssets\Shaders\Particles\particles_bitonic_sort_step.csh");
            sortShader.Use();
            sortShader.UpdateUniform("maxParticles", maxParticles);
            for (int subArraySize = 2; subArraySize >> 1 < maxParticles; subArraySize <<= 1)
            {
                for (int compareDistance = subArraySize >> 1; compareDistance > 0; compareDistance >>= 1)
                {
                    sortShader.UpdateUniform("subArraySize", subArraySize);
                    sortShader.UpdateUniform("compareDist", compareDistance);
                    sortShader.UploadUpdatedUniforms();
                    GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(kernelsCount, 1, 1);
                }
            }
        }

        private void ApplyEffect(ParticleEffect effect)
        {
            effect.Use(this);
            effect.EffectShader.UpdateUniform("deltaTime", (float)Time.DeltaTime);
            effect.EffectShader.UpdateUniform("maxParticles", maxParticles);
            effect.EffectShader.UploadUpdatedUniforms();
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(kernelsCount, 1, 1);
        }

        private int RoundUpToPowerOfTwo(int value)
        {
            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            return value + 1;
        }

        private struct Particle
        {
            public Vector3f position;
            public float energy;
            public Vector3f velocity;
        }
    }
}