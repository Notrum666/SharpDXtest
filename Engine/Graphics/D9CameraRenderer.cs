using System.Collections.Generic;

using Engine.BaseAssets.Components;

using SharpDX.DXGI;

namespace Engine.Graphics
{
    public class D9CameraRenderer
    {
        public Camera Camera { get; }

        public bool IsReady { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public nint D9SurfaceNativePointer => copyFramebuffer.D9SurfaceNativePointer;

        private D9FrameBuffer copyFramebuffer;
        private readonly HashSet<object> viewers = new HashSet<object>();

        public D9CameraRenderer(Camera camera)
        {
            Camera = camera;
            IsReady = false;

            Camera.OnResized += ResizeBuffer;
            if (!Camera.NeedsToBeResized)
                ResizeBuffer();
        }

        public void Subscribe(object viewer)
        {
            viewers.Add(viewer);

            if (viewers.Count == 1)
                EngineCore.OnFrameEnded += GameCore_OnFrameEnded;
        }

        public void Unsubscribe(object viewer)
        {
            viewers.Remove(viewer);

            if (viewers.Count == 0)
                EngineCore.OnFrameEnded -= GameCore_OnFrameEnded;
        }

        private void ResizeBuffer()
        {
            IsReady = true;
            
            Width = Camera.Width;
            Height = Camera.Height;
            copyFramebuffer = new D9FrameBuffer(Width, Height);
        }

        private void GameCore_OnFrameEnded()
        {
            FrameBuffer buffer = Camera.GetNextFrontBuffer();
            GraphicsCore.CurrentDevice.ImmediateContext.ResolveSubresource(buffer.RenderTargetTexture.texture, 0,
                                                                           copyFramebuffer.RenderTargetTexture.texture, 0, Format.B8G8R8A8_UNorm);
        }
    }
}