﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.BaseAssets.Components
{
    public abstract class Component : SerializableObject
    {
        protected virtual Type CacheType => typeof(Component);
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

        public static List<T> GetCached<T>() where T : Component
        {
            return GetCached(typeof(T)).OfType<T>().ToList();
        }

        [ProfileMe]
        public static IEnumerable<Component> GetCached(Type type)
        {
            if (!Cache.TryGetValue(type, out HashSet<Component> value))
                return [];

            return value.Where(x => x.GameObject.Enabled);
        }

        private protected override void InitializeInner()
        {
            try
            {
                OnInitialized();
                if (!Cache.TryGetValue(CacheType, out HashSet<Component> value))
                    Cache[CacheType] = value = new HashSet<Component>();
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
            if (Cache.TryGetValue(CacheType, out HashSet<Component> value))
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