using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public sealed class Framebuffer
    {
        public Texture ColorTexture { get; private set; }
        public Texture DepthTexture { get; private set; }
        public int Width { get => ColorTexture.texture.Description.Width; }
        public int Height { get => ColorTexture.texture.Description.Height; }
        private SharpDX.Direct3D9.Texture d9texture;
        public IntPtr D9SurfaceNativePointer { get => d9texture.GetSurfaceLevel(0).NativePointer; }
        public Framebuffer(Texture colorTexture, Texture depthTexture)
        {
            if (colorTexture == null)
                throw new ArgumentNullException(nameof(colorTexture));
            if (depthTexture == null)
                throw new ArgumentNullException(nameof(depthTexture));
            if (colorTexture.RenderTarget == null)
                throw new ArgumentException("Passed texture to colorTexture is not color texture");
            if (depthTexture.DepthStencil == null)
                throw new ArgumentException("Passed texture to depthTexture is not depth texture");
            if (colorTexture.texture.Description.Width != depthTexture.texture.Description.Width ||
                colorTexture.texture.Description.Height != depthTexture.texture.Description.Height)
                throw new ArgumentException("Color and depth texture size must be the same.");

            ColorTexture = colorTexture;
            DepthTexture = depthTexture;

            IntPtr renderTextureHandle = colorTexture.texture.QueryInterface<SharpDX.DXGI.Resource>().SharedHandle;
            d9texture = new SharpDX.Direct3D9.Texture(GraphicsCore.D9Device,
                                                      Width,
                                                      Height,
                                                      1,
                                                      SharpDX.Direct3D9.Usage.RenderTarget,
                                                      SharpDX.Direct3D9.Format.A8R8G8B8,
                                                      Pool.Default,
                                                      ref renderTextureHandle);
        }
    }
}
