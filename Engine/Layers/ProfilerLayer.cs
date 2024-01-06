namespace Engine.Layers
{
    internal class ProfilerLayer : Layer
    {
        public override float UpdateOrder => 4;
        public override float InitOrder => 4;

        public override void Update()
        {
            ProfilerCore.Update();
        }
    }
}
