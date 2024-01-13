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
            if (Camera.Current != null)
                //GraphicsCore.RenderShadows();
                GraphicsCore.RenderScene(Camera.Current);
        }
    }
}