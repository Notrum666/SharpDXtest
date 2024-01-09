namespace Engine.Layers
{
    internal class ProfilerLayer : Layer
    {
        public override float UpdateOrder => float.MaxValue;
        public override float InitOrder => float.MinValue;

        public override void Init()
        {
            ProfilerCore.Init();
        }

        public override void Update()
        {
            ProfilerCore.Update();
        }
    }
}