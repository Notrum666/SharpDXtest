using System;
using System.IO;

namespace Engine.AssetsData
{
    /// <summary>
    /// Does not have ImportSettings in meta, acts as ImportSettings on its own
    /// </summary>
    public abstract class NativeAssetData : AssetData
    {
        public sealed override void Serialize(BinaryWriter writer)
        {
            YamlManager.SaveToStream(writer.BaseStream, this);
        }

        public sealed override void Deserialize(BinaryReader reader)
        {
            YamlManager.LoadFromStream(reader.BaseStream, this);
        }

        public static T CreateDefault<T>() where T : NativeAssetData
        {
            T assetData = Activator.CreateInstance<T>();
            assetData.SetDefaultValues();
            return assetData;
        }

        protected abstract void SetDefaultValues();
    }
}