using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Engine;
using Engine.BaseAssets.Components;
using Engine.Layers;

namespace Editor
{
    public class EditorRuntimeLayer : Layer
    {
        public override float UpdateOrder => 1.1f;
        public override float InitOrder => 1001;

        public static event Action OnUpdate;

        public override void Update()
        {
            foreach (GameObject obj in EditorScene.GameObjects)
            {
                if (obj != null && obj.Enabled)
                    obj.Update();
            }
            
            OnUpdate?.Invoke();
        }

        public override void OnFrameEnded()
        {
            EditorScene.RemoveDestroyed();

            // foreach (Camera editorCamera in EditorScene.FindComponentsOfType<Camera>())
            // {
            //     GraphicsCore.RenderScene(editorCamera);
            // }
        }
    }

    public static class EditorScene
    {
        public static ReadOnlyCollection<GameObject> GameObjects => gameObjects.AsReadOnly();
        private static readonly List<GameObject> gameObjects = new List<GameObject>();

        internal static GameObject Instantiate(string objectName = null, Transform parent = null)
        {
            GameObject gameObject = new GameObject(true);

            gameObject.Name = objectName;
            gameObject.Transform.SetParent(parent);

            AddObject(gameObject);
            return gameObject;
        }

        internal static void AddObject(GameObject newObject)
        {
            if (gameObjects.Contains(newObject))
                return;

            gameObjects.Add(newObject);
            newObject.Initialize();
        }

        internal static void RemoveDestroyed()
        {
            gameObjects.RemoveAll(x => x == null);
        }
        
        // public static T[] FindComponentsOfType<T>(bool includeDisabledObjects = false) where T : Component
        // {
        //     return Array.ConvertAll(FindComponentsOfType(typeof(T), includeDisabledObjects), item => (T)item);
        // }
        //
        // public static Component[] FindComponentsOfType(Type type, bool includeDisabledObjects = false)
        // {
        //     if (type != typeof(Component) && !type.IsSubclassOf(typeof(Component)))
        //         return Array.Empty<Component>();
        //
        //     List<Component> foundComponents = new List<Component>();
        //     foreach (GameObject gameObject in gameObjects)
        //     {
        //         if (includeDisabledObjects || gameObject.Enabled)
        //         {
        //             foundComponents.AddRange(gameObject.GetComponents(type));
        //         }
        //     }
        //
        //     return foundComponents.ToArray();
        // }
    }
}