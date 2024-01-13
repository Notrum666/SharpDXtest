using System.Collections.Generic;

using Engine.Serialization;

using YamlDotNet.Serialization.Callbacks;

namespace Engine.AssetsData
{
    [AssetData<Scene>]
    public class SceneData : NativeAssetData
    {
        // TODO: Add total objects count for inspector
        public readonly List<SerializableObject> SerializableObjects = new List<SerializableObject>();

        protected sealed override void SetDefaultValues()
        {
            // TODO: Import/create default empty scene
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