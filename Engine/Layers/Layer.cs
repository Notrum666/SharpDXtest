namespace Engine.Layers
{
    public abstract class Layer
    {
        public abstract float UpdateOrder { get; }
        public abstract float InitOrder { get; }
        public virtual void Init() { }
        public virtual void Update() { }
    }
}
