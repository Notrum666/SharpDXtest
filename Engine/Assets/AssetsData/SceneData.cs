using System;
using System.Collections.Generic;
using System.IO;
using Engine.Serialization;
using YamlDotNet.Serialization.Callbacks;

namespace Engine.AssetsData
{
    public class SceneData : AssetData
    {
        public List<SerializedObject> SerializedObjects = new List<SerializedObject>();

        public override void Serialize(BinaryWriter writer)
        {
            YamlManager.SaveToStream(writer.BaseStream, this);
        }

        public override void Deserialize(BinaryReader reader)
        {
            YamlManager.LoadFromStream(reader.BaseStream, this);
        }

        public override Scene ToRealAsset(Type assetType)
        {
            if (assetType != typeof(Scene))
                return null;

            Scene scene = new Scene();

            foreach (SerializedObject serializedObject in SerializedObjects)
            {
                if (serializedObject is GameObject gameObject)
                    scene.AddObject(gameObject);
            }
            
            scene.ProcessNewObjects();

            return scene;
        }

        public void AddObject(SerializedObject serializedObject)
        {
            SerializedObjects.Add(serializedObject);
        }

        [OnDeserialized]
        private void OnDeserialized()
        {
            SerializedObjectPromise.InjectSerializedObjects();
        }
    }
}