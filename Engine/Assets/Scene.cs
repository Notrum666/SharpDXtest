using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;

using Engine.BaseAssets.Components;

namespace Engine
{
    public class Scene : BaseAsset
    {
        public static Scene CurrentScene { get; set; }

        public ReadOnlyCollection<GameObject> GameObjects => gameObjects.AsReadOnly();

        private readonly List<GameObject> gameObjects = new List<GameObject>();
        private readonly List<GameObject> newObjects = new List<GameObject>();

        private bool disposed = false;

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                foreach (GameObject gameObject in gameObjects.Concat(newObjects))
                {
                    gameObject.DestroyImmediate();
                }
                gameObjects.Clear();
                newObjects.Clear();
            }
            disposed = true;

            base.Dispose(disposing);
        }

        public static T[] FindComponentsOfType<T>(bool includeDisabledObjects = false) where T : Component
        {
            return Array.ConvertAll(FindComponentsOfType(typeof(T), includeDisabledObjects), item => (T)item);
        }

        public static Component[] FindComponentsOfType(Type type, bool includeDisabledObjects = false)
        {
            if (type != typeof(Component) && !type.IsSubclassOf(typeof(Component)))
                return Array.Empty<Component>();

            List<Component> foundComponents = new List<Component>();
            foreach (GameObject gameObject in CurrentScene.gameObjects)
            {
                if (includeDisabledObjects || gameObject.Enabled)
                {
                    foundComponents.AddRange(gameObject.GetComponents(type));
                }
            }

            return foundComponents.ToArray();
        }

        internal void DestroyPendingObjects()
        {
            List<SerializableObject> objectsToDestroy = new List<SerializableObject>();
            foreach (GameObject gameObject in gameObjects)
            {
                if (gameObject.PendingDestroy)
                {
                    objectsToDestroy.Add(gameObject);
                    continue;
                }

                objectsToDestroy.AddRange(gameObject.Components.Where(x => x.PendingDestroy));
            }

            foreach (SerializableObject objectToDestroy in objectsToDestroy)
            {
                if (objectToDestroy != null)
                    objectToDestroy.DestroyImmediate();
            }
        }

        internal void AddObject(GameObject newObject)
        {
            if (CurrentScene == null || CurrentScene.gameObjects.Contains(newObject) || newObjects.Contains(newObject))
                return;

            newObjects.Add(newObject);
        }

        internal void ProcessNewObjects()
        {
            foreach (GameObject newObject in newObjects.ToImmutableArray())
            {
                newObject.OnDestroyed += OnObjectDestroyed;
                gameObjects.Add(newObject);
                newObjects.Remove(newObject);
            }
        }

        private void OnObjectDestroyed(SerializableObject serializableObject)
        {
            serializableObject.OnDestroyed -= OnObjectDestroyed;
            gameObjects.Remove(serializableObject as GameObject);
        }
    }
}