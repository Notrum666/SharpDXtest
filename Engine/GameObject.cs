using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Reflection;

using Engine.BaseAssets.Components;

namespace Engine
{
    public sealed class GameObject : SerializableObject
    {
        [SerializedField]
        private string name;
        [SerializedField]
        private bool localEnabled = true;
        [SerializedField]
        private Transform transform = null;
        [SerializedField]
        private readonly List<Component> components = new List<Component>();

        public string Name { get => name; set => name = string.IsNullOrEmpty(value) ? name : value; }
        public bool LocalEnabled { get => localEnabled; set => localEnabled = value; }
        public Transform Transform { get => transform; private init => transform = value; }
        public ReadOnlyCollection<Component> Components => components.AsReadOnly();

        public bool Enabled => localEnabled && Transform != null && (Transform.Parent?.GameObject?.Enabled ?? true); //TODO: maybe better cache value

        public GameObject() : this(false) { }

        public GameObject(bool external)
        {
            Name = "NewObject";
            Transform = AddComponent<Transform>();

            if (!external && Scene.CurrentScene != null)
                Scene.CurrentScene.AddObject(this);
        }

        public static GameObject Instantiate(string objectName = null, Transform parent = null)
        {
            GameObject gameObject = Instantiate<GameObject>();

            gameObject.Name = objectName;
            gameObject.Transform.SetParent(parent);

            return gameObject;
        }

        private protected override void InitializeInner()
        {
            foreach (Component component in components)
                component.Initialize();
        }

        /// <summary>
        /// Destroys all components, handling Transform as last one.
        /// Ordering may be temporary
        /// </summary>
        private protected override void DestroyImmediateInternal()
        {
            foreach (Component component in components.ToImmutableArray())
            {
                if (component is not BaseAssets.Components.Transform)
                {
                    component.DestroyImmediate();
                }
            }

            Transform.DestroyImmediate();
            transform = null;
        }

        public T AddComponent<T>() where T : Component
        {
            return AddComponent(typeof(T)) as T;
        }

        public Component AddComponent(Type type)
        {
            if (!type.IsSubclassOf(typeof(Component)))
            {
                Logger.Log(LogType.Warning, $"Given type must be a subclass of Component, but {type.Name} was given");
                return null;
            }

            if (type.GetCustomAttribute<UniqueComponentAttribute>() is not null && GetComponent(type) is not null)
            {
                Logger.Log(LogType.Warning, $"GameObject can have only one component of type {type.Name}");
                return null;
            }

            Component newComponent = (Component)Instantiate(type);

            newComponent.GameObject = this;
            components.Add(newComponent);

            if (Initialized)
                newComponent.Initialize();
            return newComponent;
        }

        public T GetComponent<T>() where T : Component
        {
            return GetComponent(typeof(T)) as T;
        }

        public Component GetComponent(Type type)
        {
            if (!type.IsSubclassOf(typeof(Component)))
            {
                Logger.Log(LogType.Warning, $"Given type must be a subclass of Component, but {type.Name} was given");
                return null;
            }

            foreach (Component component in components)
            {
                Type componentType = component.GetType();
                if (componentType == type || componentType.IsSubclassOf(type))
                    return component;
            }

            return null;
        }

        public T[] GetComponents<T>() where T : Component
        {
            return Array.ConvertAll(GetComponents(typeof(T)), item => (T)item);
        }

        public Component[] GetComponents(Type type)
        {
            if (!type.IsSubclassOf(typeof(Component)))
            {
                Logger.Log(LogType.Warning, $"Given type must be a subclass of Component, but {type.Name} was given");
                return Array.Empty<Component>();
            }

            List<Component> typedComponents = new List<Component>();
            foreach (Component component in components)
            {
                Type componentType = component.GetType();
                if (componentType == type || componentType.IsSubclassOf(type))
                    typedComponents.Add(component);
            }
            return typedComponents.ToArray();
        }

        internal void RemoveComponent(Component component)
        {
            components.Remove(component);
        }

        public void Update()
        {
            if (!Enabled)
                return;

            foreach (Component component in components)
            {
                if (component is BehaviourComponent { LocalEnabled: true } comp)
                    comp.Update(PendingDestroy);
            }
        }

        public void FixedUpdate()
        {
            if (!Enabled)
                return;

            foreach (Component component in components)
            {
                if (component is BehaviourComponent { LocalEnabled: true } comp)
                    comp.FixedUpdate();
            }
        }
    }
}