using System;

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
                return;

            if (!started )
            {
                started = true;
                try
                {
                    Start();
                }
                catch (Exception e)
                {
                    Logger.Log(LogType.Error, $"Start() error, GameObject: {GameObject?.Name}, error: {e.Message}");
                }
            }

            try
            {
                Update();
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, $"Update() error, GameObject: {GameObject?.Name}, error: {e.Message}");
            }
        }
    }
}