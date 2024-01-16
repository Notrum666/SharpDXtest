using System;

using SharpDX.Direct3D11;

namespace Engine.Graphics
{
    public class FrameBuffer : IDisposable
    {
        public int Width => RenderTargetTexture.texture.Description.Width;
        public int Height => RenderTargetTexture.texture.Description.Height;

        public Texture RenderTargetTexture { get; private set; }

        public FrameBuffer(Texture renderTargetTexture)
        {
            if (renderTargetTexture == null)
                throw new ArgumentNullException(nameof(renderTargetTexture));

            if (!renderTargetTexture.HasViews<RenderTargetView>())
                throw new ArgumentException("Passed render target texture does not have render target view.");

            RenderTargetTexture = renderTargetTexture;
        }

        public FrameBuffer(int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));

            RenderTargetTexture = new Texture(width, height, null, SharpDX.DXGI.Format.B8G8R8A8_UNorm, BindFlags.ShaderResource | BindFlags.RenderTarget);
        }

        private bool disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                //NOTE: dispose managed state (managed objects)
            }
            //NOTE: free unmanaged resources (unmanaged objects) and override finalizer

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~FrameBuffer()
        {
            Dispose(disposing: false);
        }
    }
}