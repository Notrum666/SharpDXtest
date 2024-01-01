using System;
using System.Collections.Generic;
using System.IO;
using Engine.Serialization;
using YamlDotNet.Serialization.Callbacks;

namespace Engine.AssetsData
{
    [AssetData<Scene>]
    public class SceneData : AssetData
    {
        public List<SerializableObject> SerializableObjects = new List<SerializableObject>();

        public override void Serialize(BinaryWriter writer)
        {
            YamlManager.SaveToStream(writer.BaseStream, this);
        }

        public override void Deserialize(BinaryReader reader)
        {
            YamlManager.LoadFromStream(reader.BaseStream, this);
        }

        public override Scene ToRealAsset()
        {
            Scene scene = new Scene();

            foreach (SerializableObject serializableObject in SerializableObjects)
            {
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