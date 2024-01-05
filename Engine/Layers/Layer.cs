namespace Engine.Layers
{
    public abstract class Layer
    {
        public abstract float UpdateOrder { get; }
        public abstract float InitOrder { get; }
        public abstract void Init();
        public abstract void Update();
    }
}
