using System;

namespace Engine.BaseAssets.Components
{
    public abstract class Component : SerializableObject
    {
        [SerializedField]
        private GameObject gameObject = null;

        public GameObject GameObject
        {
            get => gameObject;
            set
            {
                if (gameObject != null)
                {
                    Logger.Log(LogType.Warning, "Tried to set GameObject of Component multiple times.");
                    return;
                }
                gameObject = value;
            }
        }

        private protected override void InitializeInner()
        {
            try
            {
                OnInitialized();
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, $"OnInitialized() error, GameObject: {GameObject?.Name}, error: {e.Message}");
            }
        }

        /// <summary>
        /// Calls OnDestroy and removes GameObject linking
        /// </summary>
        private protected override void DestroyImmediateInternal()
        {
            try
            {
                OnDestroy();
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, $"OnDestroy() error, GameObject: {GameObject?.Name}, error: {e.Message}");
            }
            gameObject.RemoveComponent(this);
            gameObject = null;
        }

        /// <summary>
        /// Called immediately after being added to GameObject
        /// </summary>
        protected virtual void OnInitialized() { }

        /// <summary>
        /// If called as result of upper hierarchy object destroy, all upper objects may be already invalid
        /// </summary>
        protected virtual void OnDestroy() { }
    }
}