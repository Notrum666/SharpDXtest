namespace Engine.BaseAssets.Components
{
    public abstract class BehaviourComponent : Component
    {
        [SerializedField]
        private bool localEnabled = true;

        public bool LocalEnabled { get => localEnabled; set => localEnabled = value; }
        public bool Enabled => LocalEnabled && GameObject.Enabled;

        private bool started = false;

        /// <summary>
        /// Called before first Update
        /// </summary>
        public virtual void Start() { }

        public virtual void Update() { }

        public virtual void FixedUpdate() { }

        internal void Update(bool gameObjectPendingDestroy)
        {
            if (gameObjectPendingDestroy || PendingDestroy)
            {
                OnDestroy();
                return;
            }

            if (!started )
            {
                started = true;
                Start();
            }

            Update();
        }
    }
}