using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDXtest.BaseAssets.Components;

namespace SharpDXtest
{
    public sealed class GameObject
    {
        private bool enabled = true;
        public bool Enabled { get => transform.Parent == null ? enabled : enabled && transform.Parent.gameObject.enabled; set => enabled = value; }
        public Transform transform { get; }
        private List<Component> components = new List<Component>();
        public ReadOnlyCollection<Component> Components { get => components.AsReadOnly(); }
        public GameObject()
        {
            transform = new Transform();
            transform.gameObject = this;
            components.Add(transform);
        }

        public T addComponent<T>() where T : Component
        {
            Component component = Activator.CreateInstance(typeof(T)) as Component;
            component.gameObject = this;
            components.Add(component);
            return component as T;
        }
        public Component addComponent(Type t)
        {
            if (!t.IsSubclassOf(typeof(Component)))
                throw new ArgumentException("Given type must be a component");
            Component component = Activator.CreateInstance(t) as Component;
            component.gameObject = this;
            components.Add(component);
            return component;
        }
        public T getComponent<T>() where T : Component
        {
            foreach (Component component in components)
                if (component is T)
                    return component as T;
            return null;
        }
        public T[] getComponents<T>() where T : Component
        {
            List<T> curComponents = new List<T>();
            foreach (Component component in components)
                if (component is T)
                    curComponents.Add(component as T);
            return curComponents.ToArray();
        }
        public Component getComponent(Type t)
        {
            if (!t.IsSubclassOf(typeof(Component)))
                throw new ArgumentException("Given type must be a component");
            foreach (Component component in components)
                if (component.GetType() == t || component.GetType().IsSubclassOf(t))
                    return component;
            return null;
        }
        public Component[] getComponents(Type t)
        {
            if (!t.IsSubclassOf(typeof(Component)))
                throw new ArgumentException("Given type must be a component");
            List<Component> curComponents = new List<Component>();
            foreach (Component component in components)
                if (component.GetType() == t || component.GetType().IsSubclassOf(t))
                    curComponents.Add(component);
            return curComponents.ToArray();
        }
        public void update()
        {
            foreach (Component component in components)
                if (component.Enabled)
                    component.update();
        }
        public void fixedUpdate()
        {
            foreach (Component component in components)
                if (component.Enabled)
                    component.fixedUpdate();
        }
    }
}
