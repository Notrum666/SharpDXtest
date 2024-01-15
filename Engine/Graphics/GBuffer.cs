using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Engine.Graphics
{
    readonly struct GBuffer
    {
        public readonly Texture WorldPos;
        public readonly Texture Albedo;
        public readonly Texture Normal;
        public readonly Texture Metallic;
        public readonly Texture Roughness;
        public readonly Texture AmbientOcclusion;
        public readonly Texture Emission;

        public GBuffer(int width, int height)
        {
            WorldPos = new Texture(width, height, null, Format.R32G32B32A32_Float, BindFlags.RenderTarget | BindFlags.ShaderResource);
            Albedo = new Texture(width, height, null, Format.R32G32B32A32_Float, BindFlags.RenderTarget | BindFlags.ShaderResource);
            Normal = new Texture(width, height, null, Format.R32G32B32A32_Float, BindFlags.RenderTarget | BindFlags.ShaderResource);
            Metallic = new Texture(width, height, null, Format.R32_Typeless, BindFlags.RenderTarget | BindFlags.ShaderResource);
            Roughness = new Texture(width, height, null, Format.R32_Typeless, BindFlags.RenderTarget | BindFlags.ShaderResource);
            AmbientOcclusion = new Texture(width, height, null, Format.R32_Typeless, BindFlags.RenderTarget | BindFlags.ShaderResource);
            Emission = new Texture(width, height, null, Format.R32_Typeless, BindFlags.RenderTarget | BindFlags.ShaderResource);
        }

        internal void Dispose()
        {
            WorldPos?.Dispose();
            Albedo?.Dispose();
            Normal?.Dispose();
            Metallic?.Dispose();
            Roughness?.Dispose();
            AmbientOcclusion?.Dispose();
            Emission?.Dispose();
        }
    }
}