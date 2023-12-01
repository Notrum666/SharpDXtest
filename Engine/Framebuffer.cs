using System;
using System.Linq;

using SharpDX.Direct3D11;
using SharpDX.Direct3D9;

using Resource = SharpDX.DXGI.Resource;

namespace Engine
{
    public sealed class FrameBuffer : IDisposable
    {
        public Texture RenderTargetTexture { get; private set; }
        public Texture DepthTexture { get; private set; }
        public int Width => RenderTargetTexture.texture.Description.Width;
        public int Height => RenderTargetTexture.texture.Description.Height;
        private SharpDX.Direct3D9.Texture d9texture;
        private bool disposed;

        public nint D9SurfaceNativePointer => d9texture.GetSurfaceLevel(0).NativePointer;

        public FrameBuffer(Texture renderTargetTexture, Texture depthTexture)
        {
            if (renderTargetTexture == null)
                throw new ArgumentNullException(nameof(renderTargetTexture));
            if (depthTexture == null)
                throw new ArgumentNullException(nameof(depthTexture));

            if (!renderTargetTexture.HasViews<RenderTargetView>())
                throw new ArgumentException("Passed render target texture does not have render target view.");
            if (!depthTexture.HasViews<DepthStencilView>())
                throw new ArgumentException("Passed depth texture does not have depth stencil view.");

            if (renderTargetTexture.texture.Description.Width != depthTexture.texture.Description.Width ||
                renderTargetTexture.texture.Description.Height != depthTexture.texture.Description.Height)
                throw new ArgumentException("Render target and depth texture size must be the same.");

            RenderTargetTexture = renderTargetTexture;
            DepthTexture = depthTexture;

            nint renderTextureHandle = renderTargetTexture.texture.QueryInterface<Resource>().SharedHandle;
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

            RenderTargetTexture = new Texture(width, height, null, SharpDX.DXGI.Format.B8G8R8A8_UNorm, BindFlags.ShaderResource | BindFlags.RenderTarget);
            DepthTexture = new Texture(width, height, null, SharpDX.DXGI.Format.R32_Typeless, BindFlags.DepthStencil);

            nint renderTextureHandle = RenderTargetTexture.texture.QueryInterface<Resource>().SharedHandle;
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