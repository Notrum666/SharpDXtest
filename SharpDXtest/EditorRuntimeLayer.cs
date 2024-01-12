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

        public override void Update()
        {
            foreach (GameObject obj in EditorScene.GameObjects)
            {
                if (obj != null && obj.Enabled)
                    obj.Update();
            }

            EditorScene.RemoveDestroyed();
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
    }
}