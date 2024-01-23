using System.Collections.Generic;
using System.IO;

using Engine.Assets;
using Engine.BaseAssets.Components;
using Engine.Serialization;

using YamlDotNet.Serialization.Callbacks;

namespace Engine.AssetsData
{
    [AssetData<Prefab>]
    public class PrefabData : NativeAssetData
    {
        public sealed override string FileExtension => ".prefab";

        // TODO: Add total objects count for inspector
        public readonly List<SerializableObject> SerializableObjects = new List<SerializableObject>();

        protected sealed override void SetDefaultValues()
        {
            // TODO: Import/create default empty scene
        }

        public static PrefabData FromGameObject(GameObject rootObject)
        {
            PrefabData prefabData = new PrefabData();

            Stack<Transform> stack = new Stack<Transform>();
            stack.Push(rootObject.Transform);

            while (stack.Count > 0)
            {
                Transform transform = stack.Pop();
                foreach (Transform child in transform.Children)
                    stack.Push(child);

                GameObject gameObject = transform.GameObject;
                prefabData.SerializableObjects.Add(gameObject);
                foreach (Component component in gameObject.Components)
                    prefabData.SerializableObjects.Add(component);
            }

            return prefabData;
        }

        public sealed override void Serialize(BinaryWriter writer)
        {
            YamlManager.SaveToStream(writer.BaseStream, this);
        }

        public sealed override void Deserialize(BinaryReader reader)
        {
            YamlManager.LoadFromStream(reader.BaseStream, this);
        }

        public override Prefab ToRealAsset(BaseAsset targetAsset = null)
        {
            if (targetAsset != null)
                Logger.Log(LogType.Warning, "Tried to update Scene asset, but it is not allowed, cause asset is not cacheable");

            Prefab prefab = new Prefab();

            foreach (SerializableObject serializableObject in SerializableObjects)
            {
                serializableObject.OnDeserialized();
                if (serializableObject is GameObject gameObject)
                    prefab.AddObject(gameObject);
            }
            prefab.SetRootTransform(SerializableObjects[0] as Transform); //Cross fingers
            // prefab.ProcessNewObjects();

            return prefab;
        }

        public void AddObject(SerializableObject serializableObject)
        {
            SerializableObjects.Add(serializableObject);
        }

        [OnDeserialized]
        private void OnDeserialized()
        {
            SerializedObjectPromise.InjectSerializedObjects();
        }
    }
}