using System;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;

namespace Engine
{
    public class Sampler : IDisposable
    {
        private SamplerState sampler;
        private bool disposed;

        #region Legacy

        public static Sampler Default => new Sampler(TextureAddressMode.Wrap, TextureAddressMode.Wrap);
        public static Sampler DefaultShadows => new Sampler(TextureAddressMode.Border, TextureAddressMode.Border, Filter.ComparisonMinMagMipLinear, 0, new RawColor4(0.0f, 0.0f, 0.0f, 0.0f), Comparison.LessEqual);

        #endregion Legacy

        public Sampler(TextureAddressMode addressU, TextureAddressMode addressV, Filter filter = Filter.Anisotropic, int maximumAnisotropy = 8, RawColor4 borderColor = new RawColor4(), Comparison comparisonFunction = Comparison.Always, TextureAddressMode addressW = TextureAddressMode.Clamp)
        {
            sampler = new SamplerState(GraphicsCore.CurrentDevice, new SamplerStateDescription()
            {
                AddressU = addressU,
                AddressV = addressV,
                AddressW = addressW,
                Filter = filter,
                MaximumAnisotropy = maximumAnisotropy,
                MipLodBias = 0,
                MinimumLod = 0,
                MaximumLod = float.MaxValue,
                BorderColor = borderColor,
                ComparisonFunction = comparisonFunction
            });
        }

        public void use(string variable)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(Sampler));
            bool correctLocation = false;
            int location;
            foreach (Shader shader in ShaderPipeline.Current.Shaders)
            {
                if (shader.Locations.TryGetValue(variable, out location))
                {
                    correctLocation = true;
                    GraphicsCore.CurrentDevice.ImmediateContext.PixelShader.SetSampler(location, sampler);
                }
            }
            if (!correctLocation)
                throw new ArgumentException("Variable " + variable + " not found in current pipeline.");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                sampler.Dispose();

                disposed = true;
            }
        }

        ~Sampler()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}