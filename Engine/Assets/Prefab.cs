using System.Collections.Generic;

using Engine.BaseAssets.Components;

namespace Engine.Assets
{
    public class Prefab : BaseAsset
    {
        private Transform rootTransform;
        private readonly List<GameObject> gameObjects = new List<GameObject>();

        internal void SetRootTransform(Transform transform)
        {
            rootTransform = transform;
        }

        internal void AddObject(GameObject newObject)
        {
            if (gameObjects.Contains(newObject))
                return;

            gameObjects.Add(newObject);
        }

        private void MakeNewGuids()
        {
            foreach (GameObject gameObject in gameObjects)
            {
                gameObject.MakeNewGuid();
                foreach (Component component in gameObject.Components)
                {
                    component.MakeNewGuid();
                }
            }
        }

        public GameObject Instantiate(Transform parentTransform = null)
        {
            MakeNewGuids();

            Scene currentScene = Scene.CurrentScene;
            if (currentScene != null)
            {
                foreach (var gameObject in gameObjects)
                {
                    currentScene.AddObject(gameObject);
                }
            }

            if (parentTransform != null)
                rootTransform.SetParent(parentTransform);
            return rootTransform.GameObject;
        }
    }
}