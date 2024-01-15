using System.Windows;
using System.Windows.Interop;

using Engine;
using Engine.BaseAssets.Components;
using Engine.Graphics;

namespace Editor
{
    public class CameraViewModel : ViewModelBase
    {
        public int FPS
        {
            get => fps;
            set
            {
                fps = value;
                OnPropertyChanged();
            }
        }

        private double timeCounter = 0.0;
        private int framesCount = 0;
        private int fps = -1;

        private D9CameraRenderer D9Renderer => camera?.D9Renderer;
        private Camera camera;

        public static void ResizeCamera(Camera camera, int width, int height)
        {
            if (camera == null)
                return;

            camera.Aspect = width / (double)height;
            camera.Resize(width, height);
        }

        public void SetCamera(Camera newCamera)
        {
            if (camera != null)
            {
                D9Renderer.Unsubscribe(this);
            }
            EngineCore.OnFrameEnded -= GameCore_OnFrameEnded;
            FPS = -1;

            camera = newCamera;
            if (camera != null)
            {
                D9Renderer.Subscribe(this);
                EngineCore.OnFrameEnded += GameCore_OnFrameEnded;
            }
        }

        public void Render(D3DImage targetImage)
        {
            targetImage.Lock();

            targetImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, D9Renderer.D9SurfaceNativePointer);
            targetImage.AddDirtyRect(new Int32Rect(0, 0, D9Renderer.Width, D9Renderer.Height));

            targetImage.Unlock();
        }

        public void ResizeCamera(int width, int height)
        {
            ResizeCamera(camera, width, height);
        }

        private void GameCore_OnFrameEnded()
        {
            if (!EngineCore.IsAlive || camera == null)
            {
                if (FPS != -1)
                    FPS = -1;
                return;
            }

            timeCounter += Time.DeltaTime;
            framesCount++;
            if (timeCounter >= 1.0)
            {
                FPS = framesCount;

                timeCounter -= 1.0;
                framesCount = 0;
            }
        }
    }
}