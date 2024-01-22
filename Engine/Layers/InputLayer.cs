namespace Engine.Layers
{
    internal class InputLayer : Layer
    {
        public override float UpdateOrder => 0;
        public override float InitOrder => 0;

        public override void Init()
        {
            Input.Init();
        }

        public override void FixedUpdate()
        {
            Input.FixedUpdate();
        }

        public override void Update()
        {
            Input.Update();
        }
    }
}