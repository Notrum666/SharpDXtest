namespace Engine.Layers
{
    internal class InputLayer : Layer
    {
        public override float UpdateOrder => 0;
        public override float InitOrder => 0;

        public override void Init()
        {
            InputManager.Init();
        }

        public override void Update()
        {
            InputManager.Update();
        }
    }
}