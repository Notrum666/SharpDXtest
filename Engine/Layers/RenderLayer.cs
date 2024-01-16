using System.Linq;

using Engine.BaseAssets.Components;

namespace Engine.Layers
{
    internal class RenderLayer : Layer
    {
        public override float UpdateOrder => 3;
        public override float InitOrder => 3;

        public override void Init()
        {
            GraphicsCore.Init();
        }

        public override void Update()
        {
            foreach (Camera camera in Camera.Cameras.Where(x => x.Enabled))
            {
                //GraphicsCore.RenderShadows();
                GraphicsCore.RenderScene(camera);
            }
        }

        public override void OnFrameEnded()
        {
            foreach (Camera camera in Camera.Cameras.Where(x => x.Enabled))
            {
                camera.DrawFrontBuffer();
            }
        }
    }
}