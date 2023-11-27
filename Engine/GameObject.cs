using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.BaseAssets.Components;

namespace Engine
{
    public sealed class GameObject
    {
        private bool enabled = true;
        public bool Enabled { get => Transform.Parent == null ? enabled : enabled && Transform.Parent.GameObject.enabled; set => enabled = value; }
        public Transform Transform { get; private set; }
        private List<Component> components = new List<Component>();
        public ReadOnlyCollection<Component> Components => components.AsReadOnly();
        internal bool PendingDestroy { get; private set; }
        public bool Initialized { get; internal set; }

        public GameObject()
        {
            Transform = new Transform();
            Transform.GameObject = this;
            components.Add(Transform);
        }

        public T AddComponent<T>() where T : Component
        {
            Component component = Activator.CreateInstance(typeof(T)) as Component;
            component.GameObject = this;
            components.Add(component);
            if (Initialized)
                component.Initialize();
            return component as T;
        }

        public Component AddComponent(Type t)
        {
            if (!t.IsSubclassOf(typeof(Component)))
                throw new ArgumentException("Given type must be a component");
            Component component = Activator.CreateInstance(t) as Component;
            component.GameObject = this;
            components.Add(component);
            if (Initialized)
                component.Initialize();
            return component;
        }

        public T GetComponent<T>() where T : Component
        {
            foreach (Component component in components)
                if (component is T)
                    return component as T;
            return null;
        }

        public T[] GetComponents<T>() where T : Component
        {
            List<T> curComponents = new List<T>();
            foreach (Component component in components)
                if (component is T)
                    curComponents.Add(component as T);
            return curComponents.ToArray();
        }

        public Component GetComponent(Type t)
        {
            if (!t.IsSubclassOf(typeof(Component)))
                throw new ArgumentException("Given type must be a component");
            foreach (Component component in components)
                if (component.GetType() == t || component.GetType().IsSubclassOf(t))
                    return component;
            return null;
        }

        public Component[] GetComponents(Type t)
        {
            if (!t.IsSubclassOf(typeof(Component)))
                throw new ArgumentException("Given type must be a component");
            List<Component> curComponents = new List<Component>();
            foreach (Component component in components)
                if (component.GetType() == t || component.GetType().IsSubclassOf(t))
                    curComponents.Add(component);
            return curComponents.ToArray();
        }

        public void Initialize()
        {
            if (Initialized)
                return;
            Initialized = true;
            foreach (Component component in components)
                component.Initialize();
        }

        public void Update()
        {
            foreach (Component component in components)
                if (component.Enabled)
                    component.Update();
        }

        public void FixedUpdate()
        {
            foreach (Component component in components)
                if (component.Enabled)
                    component.FixedUpdate();
        }

        public void Destroy()
        {
            PendingDestroy = true;
        }
    }
}