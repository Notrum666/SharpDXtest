using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.BaseAssets.Components
{
    public abstract class Component : SerializableObject
    {
        public static Dictionary<Type, HashSet<Component>> Cache { get; } = new Dictionary<Type, HashSet<Component>>();

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

        public static T[] GetCached<T>() where T : Component
        {
            Type type = typeof(T);
            if (!Cache.TryGetValue(type, out HashSet<Component> value))
                return [];

            return Array.ConvertAll(value.ToArray(), item => (T)item);
        }

        private protected override void InitializeInner()
        {
            try
            {
                OnInitialized();
                Type type = GetType();
                if (!Cache.TryGetValue(type, out HashSet<Component> value))
                    value = Cache[type] = new HashSet<Component>();
                value.Add(this);
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
            Type type = GetType();
            if (Cache.TryGetValue(type, out HashSet<Component> value))
                value.Remove(this);

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