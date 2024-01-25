using System.Collections.Generic;
using System.IO;
using System.Text;
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
            var sw = new StreamWriter(writer.BaseStream, Encoding.UTF8);
            sw.Write(Data);
            sw.Flush();
        }

        public sealed override void Deserialize(BinaryReader reader)
        {
            reader.BaseStream.Position = 0;
            using (StreamReader streamReader = new StreamReader(reader.BaseStream, Encoding.UTF8))
            {
                Data = streamReader.ReadToEnd();
            }
        }

        public override Prefab ToRealAsset(BaseAsset targetAsset = null)
        {
            if (targetAsset != null)
                Logger.Log(LogType.Warning, "Tried to update Scene asset, but it is not allowed, cause asset is not cacheable");

            Prefab prefab = new Prefab();
            prefab.SetPrefabData(this);

            return prefab;
        }
    }
}