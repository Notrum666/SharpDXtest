using System.Collections.Generic;
using System.IO;

using Engine.BaseAssets.Components;
using Engine.Serialization;

using YamlDotNet.Serialization.Callbacks;

namespace Engine.AssetsData
{
    [AssetData<Scene>]
    public class SceneData : NativeAssetData
    {
        public sealed override string FileExtension => ".scene";

        // TODO: Add total objects count for inspector
        public readonly List<SerializableObject> SerializableObjects = new List<SerializableObject>();

        protected sealed override void SetDefaultValues()
        {
            // TODO: Import/create default empty scene
        }

        public static SceneData FromScene(Scene scene)
        {
            SceneData sceneData = new SceneData();
            foreach (GameObject gameObject in scene.GameObjects)
            {
                sceneData.SerializableObjects.Add(gameObject);
                foreach (Component component in gameObject.Components)
                    sceneData.SerializableObjects.Add(component);
            }
            return sceneData;
        }

        public sealed override void Serialize(BinaryWriter writer)
        {
            YamlManager.SaveToStream(writer.BaseStream, this);
        }

        public sealed override void Deserialize(BinaryReader reader)
        {
            YamlManager.LoadFromStream(reader.BaseStream, this);
        }

        public override Scene ToRealAsset()
        {
            Scene scene = new Scene();

            foreach (SerializableObject serializableObject in SerializableObjects)
            {
                serializableObject.OnDeserialized();
                if (serializableObject is GameObject gameObject)
                    scene.AddObject(gameObject);
            }

            scene.ProcessNewObjects();

            return scene;
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