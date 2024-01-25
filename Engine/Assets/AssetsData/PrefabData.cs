using System.Collections.Generic;
using System.IO;

using Engine.Assets;
using Engine.BaseAssets.Components;
using Engine.Serialization;

using YamlDotNet.Serialization.Callbacks;

namespace Engine.AssetsData
{
    [AssetData<Prefab>][YamlTagMapped("OldPrefabData")]
    public class PrefabData : NativeAssetData
    {
        public sealed override string FileExtension => ".prefab";

        public string Data;
        

        // TODO: Add total objects count for inspector
        // public readonly List<SerializableObject> SerializableObjects = new List<SerializableObject>();
        // public int rootTransformIndex = -1;

        protected sealed override void SetDefaultValues()
        {
            // TODO: Import/create default empty scene
        }

        public static PrefabData FromGameObject(GameObject rootObject)
        {
            PrefabData prefabData = new PrefabData();

            PrefabContainer prefabContainer = new PrefabContainer();
            Stack<Transform> stack = new Stack<Transform>();
            stack.Push(rootObject.Transform);
            
            while (stack.Count > 0)
            {
                Transform transform = stack.Pop();
                foreach (Transform child in transform.Children)
                    stack.Push(child);
                GameObject gameObject = transform.GameObject;

                prefabContainer.SerializableObjects.Add(gameObject);
                foreach (Component component in gameObject.Components)
                {
                    prefabContainer.SerializableObjects.Add(component);
                    if (component == rootObject.Transform)
                        prefabContainer.rootTransformIndex = prefabContainer.SerializableObjects.Count - 1;
                }
            }

            using StringWriter stringWriter = new StringWriter();
            
            prefabData.Data = YamlManager.SaveToString(prefabContainer);
            
            return prefabData;
        }

        public sealed override void Serialize(BinaryWriter writer)
        {
            writer.Write(Data);
            // YamlManager.SaveToStream(writer.BaseStream, this);
        }

        public sealed override void Deserialize(BinaryReader reader)
        {
            Data = reader.ReadString();
            // YamlManager.LoadFromStream(reader.BaseStream, this);
        }

        public override Prefab ToRealAsset(BaseAsset targetAsset = null)
        {
            if (targetAsset != null)
                Logger.Log(LogType.Warning, "Tried to update Scene asset, but it is not allowed, cause asset is not cacheable");

            Prefab prefab = new Prefab();
            prefab.SetPrefabData(this);

            // foreach (SerializableObject serializableObject in SerializableObjects)
            // {
            //     serializableObject.OnDeserialized();
            //     if (serializableObject is GameObject gameObject)
            //         prefab.AddObject(gameObject);
            // }
            // prefab.SetRootTransform(SerializableObjects[rootTransformIndex] as Transform); //Cross fingers

            return prefab;
        }

        // public void AddObject(SerializableObject serializableObject)
        // {
        //     SerializableObjects.Add(serializableObject);
        // }

        [OnDeserialized]
        private void OnDeserialized()
        {
            SerializedObjectPromise.InjectSerializedObjects();
        }
    }
}