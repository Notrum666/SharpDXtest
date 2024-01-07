using System.IO;

namespace Engine.AssetsData
{
    public class ScriptData : AssetData
    {
        public override void Serialize(BinaryWriter writer)
        {
            YamlManager.SaveToStream(writer.BaseStream, this);
        }

        public override void Deserialize(BinaryReader reader)
        {
            YamlManager.LoadFromStream(reader.BaseStream, this);
        }

        public override ScriptAsset ToRealAsset()
        {
            return null;
        }
    }
}