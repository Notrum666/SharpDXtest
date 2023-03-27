using SharpDX.Direct3D11;
using SharpDX.Direct3D9;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public sealed class FrameBuffer : IDisposable
    {
        public Texture RenderTargetTexture { get; private set; }
        public Texture DepthTexture { get; private set; }
        public int Width { get => RenderTargetTexture.texture.Description.Width; }
        public int Height { get => RenderTargetTexture.texture.Description.Height; }
        private SharpDX.Direct3D9.Texture d9texture;
        private bool disposed;

        public IntPtr D9SurfaceNativePointer { get => d9texture.GetSurfaceLevel(0).NativePointer; }
        public FrameBuffer(Texture renderTargetTexture, Texture depthTexture)
        {
            if (renderTargetTexture == null)
                throw new ArgumentNullException(nameof(renderTargetTexture));
            if (depthTexture == null)
                throw new ArgumentNullException(nameof(depthTexture));

            if (!renderTargetTexture.Views.Any(view => view is RenderTargetView))
                throw new ArgumentException("Passed render target texture does not have render target view.");
            if (!depthTexture.Views.Any(view => view is DepthStencilView))
                throw new ArgumentException("Passed depth texture does not have depth stencil view.");

            if (renderTargetTexture.texture.Description.Width != depthTexture.texture.Description.Width ||
                renderTargetTexture.texture.Description.Height != depthTexture.texture.Description.Height)
                throw new ArgumentException("Render target and depth texture size must be the same.");

            RenderTargetTexture = renderTargetTexture;
            DepthTexture = depthTexture;

            IntPtr renderTextureHandle = renderTargetTexture.texture.QueryInterface<SharpDX.DXGI.Resource>().SharedHandle;
            d9texture = new SharpDX.Direct3D9.Texture(GraphicsCore.D9Device,
                                                      Width,
                                                      Height,
                                                      1,
                                                      Usage.RenderTarget,
                                                      Format.A8R8G8B8,
                                                      Pool.Default,
                                                      ref renderTextureHandle);
        }
        public FrameBuffer(int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));

            RenderTargetTexture = new Texture(width, height, new byte[] { 0, 0, 0, 0 }, SharpDX.DXGI.Format.B8G8R8A8_UNorm, BindFlags.ShaderResource | BindFlags.RenderTarget);
            DepthTexture = new Texture(width, height, 0.0f.GetBytes(), SharpDX.DXGI.Format.R32_Typeless, BindFlags.DepthStencil);

            IntPtr renderTextureHandle = RenderTargetTexture.texture.QueryInterface<SharpDX.DXGI.Resource>().SharedHandle;
            d9texture = new SharpDX.Direct3D9.Texture(GraphicsCore.D9Device,
                                                      Width,
                                                      Height,
                                                      1,
                                                      Usage.RenderTarget,
                                                      Format.A8R8G8B8,
                                                      Pool.Default,
                                                      ref renderTextureHandle);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                d9texture.Dispose();

                disposed = true;
            }
        }
        ~FrameBuffer()
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
