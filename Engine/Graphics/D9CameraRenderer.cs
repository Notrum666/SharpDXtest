using System.Collections.Generic;

using Engine.BaseAssets.Components;

using SharpDX.DXGI;

namespace Engine.Graphics
{
    public class D9CameraRenderer
    {
        public Camera Camera { get; }
        public bool IsInitialized { get; private set; }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public nint D9SurfaceNativePointer => copyFramebuffer.D9SurfaceNativePointer;

        internal int ViewersCount => viewers.Count;

        private readonly HashSet<object> viewers = new HashSet<object>();
        private D9FrameBuffer copyFramebuffer;

        public D9CameraRenderer(Camera camera)
        {
            Camera = camera;
            IsInitialized = false;

            Camera.OnResized += ResizeBuffer;
            if (!Camera.NeedsToBeResized)
                ResizeBuffer();
        }

        private void ResizeBuffer()
        {
            Width = Camera.Width;
            Height = Camera.Height;
            copyFramebuffer = new D9FrameBuffer(Width, Height);

            IsInitialized = true;
        }

        public void Subscribe(object viewer)
        {
            viewers.Add(viewer);
        }

        public void Unsubscribe(object viewer)
        {
            viewers.Remove(viewer);
        }

        internal void Draw(FrameBuffer buffer)
        {
            if (!IsInitialized)
                return;

            // FrameBuffer buffer = Camera.GetNextFrontBuffer();
            GraphicsCore.CurrentDevice.ImmediateContext.ResolveSubresource(buffer.RenderTargetTexture.texture, 0,
                                                                           copyFramebuffer.RenderTargetTexture.texture, 0, Format.B8G8R8A8_UNorm);
        }
    }
}