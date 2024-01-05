namespace Engine.Layers
{
    internal class SoundLayer : Layer
    {
        public override float UpdateOrder => 2;

        public override float InitOrder => 2;

        public override void Init() { }

        public override void Update()
        {
            SoundCore.Update();
        }
    }
}
