namespace Engine.Layers
{
    internal class SoundLayer : Layer
    {
        public override float UpdateOrder => 2;
        public override float InitOrder => 2;

        public override void Update()
        {
            if (EngineCore.IsPaused)
                return;

            SoundCore.Update();
        }
    }
}
