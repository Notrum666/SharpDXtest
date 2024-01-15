using SharpDX.Direct3D9;

using Resource = SharpDX.DXGI.Resource;

namespace Engine.Graphics
{
    public class D9FrameBuffer : FrameBuffer
    {
        public nint D9SurfaceNativePointer => d9Texture.GetSurfaceLevel(0).NativePointer;

        private readonly SharpDX.Direct3D9.Texture d9Texture;

        private bool disposed = false;

        public D9FrameBuffer(Texture renderTargetTexture) : base(renderTargetTexture)
        {
            nint renderTextureHandle = renderTargetTexture.texture.QueryInterface<Resource>().SharedHandle;
            d9Texture = new SharpDX.Direct3D9.Texture(GraphicsCore.D9Device,
                                                      Width,
                                                      Height,
                                                      1,
                                                      Usage.RenderTarget,
                                                      Format.A8R8G8B8,
                                                      Pool.Default,
                                                      ref renderTextureHandle);
        }

        public D9FrameBuffer(int width, int height) : base(width, height)
        {
            nint renderTextureHandle = RenderTargetTexture.texture.QueryInterface<Resource>().SharedHandle;
            d9Texture = new SharpDX.Direct3D9.Texture(GraphicsCore.D9Device,
                                                      Width,
                                                      Height,
                                                      1,
                                                      Usage.RenderTarget,
                                                      Format.A8R8G8B8,
                                                      Pool.Default,
                                                      ref renderTextureHandle);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                //NOTE: dispose managed state (managed objects)
                d9Texture.Dispose();
            }
            //NOTE: free unmanaged resources (unmanaged objects) and override finalizer

            disposed = true;
            base.Dispose(disposing);
        }
    }
}