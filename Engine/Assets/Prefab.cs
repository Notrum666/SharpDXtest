using System.Collections.Generic;
using System.IO;

using Engine.AssetsData;
using Engine.BaseAssets.Components;
using Engine.Serialization;

using YamlDotNet.Serialization.Callbacks;

namespace Engine.Assets
{
    public class Prefab : BaseAsset
    {
        private PrefabData prefabData;
        
        // private Transform rootTransform;
        // private readonly List<GameObject> gameObjects = new List<GameObject>();

        internal void SetPrefabData(PrefabData data)
        {
            prefabData = data;
        }
        
        // internal void SetRootTransform(Transform transform)
        // {
        //     rootTransform = transform;
        // }
        //
        // internal void AddObject(GameObject newObject)
        // {
        //     if (gameObjects.Contains(newObject))
        //         return;
        //
        //     gameObjects.Add(newObject);
        // }
        //
        // private void MakeNewGuids()
        // {
        //     foreach (GameObject gameObject in gameObjects)
        //     {
        //         gameObject.MakeNewGuid();
        //         foreach (Component component in gameObject.Components)
        //         {
        //             component.MakeNewGuid();
        //         }
        //     }
        // }

        // public GameObject Instantiate(Transform parentTransform = null)
        // {
        //     MakeNewGuids();
        //
        //     Scene currentScene = Scene.CurrentScene;
        //     if (currentScene != null)
        //     {
        //         foreach (var gameObject in gameObjects)
        //         {
        //             currentScene.AddObject(gameObject);
        //         }
        //     }
        //
        //     if (parentTransform != null)
        //         rootTransform.SetParent(parentTransform);
        //     return rootTransform.GameObject;
        // }

        public GameObject NewInstantiate(Transform parentTransform = null)
        {
            List<GameObject> newObjects = new List<GameObject>();
            Transform rootTrans;
            
            PrefabContainer container = PrefabContainer.Deserialize(prefabData.Data);
            foreach (SerializableObject serializableObject in container.SerializableObjects)
            {
                serializableObject.OnDeserialized();
                if (serializableObject is GameObject gameObject)
                    newObjects.Add(gameObject);
            }
            rootTrans = (container.SerializableObjects[container.rootTransformIndex] as Transform)!; //Cross fingers
            
            MakeNewGuids(newObjects);
            
            Scene currentScene = Scene.CurrentScene;
            if (currentScene != null)
            {
                foreach (GameObject gameObject in newObjects)
                {
                    currentScene.AddObject(gameObject);
                }
            }

            if (parentTransform != null)
                rootTrans.SetParent(parentTransform);
            return rootTrans.GameObject;
        }
        
        private void MakeNewGuids(List<GameObject> newObjects)
        {
            foreach (GameObject gameObject in newObjects)
            {
                gameObject.MakeNewGuid();
                foreach (Component component in gameObject.Components)
                {
                    component.MakeNewGuid();
                }
            }
        }
    }

    [YamlTagMapped("PrefabData")]
    internal class PrefabContainer
    {
        public readonly List<SerializableObject> SerializableObjects = new List<SerializableObject>();
        public int rootTransformIndex = -1;
        
        // public void Serialize(BinaryWriter writer)
        // {
        //     YamlManager.SaveToStream(writer.BaseStream, this);
        // }

        public static PrefabContainer Deserialize(string data)
        {
            return YamlManager.LoadFromString<PrefabContainer>(data);
        }
        
        [OnDeserialized]
        private void OnDeserialized()
        {
            SerializedObjectPromise.InjectSerializedObjects();
        }
    }
}