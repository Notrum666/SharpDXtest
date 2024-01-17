using System;
using System.IO;

namespace Engine.AssetsData
{
    /// <summary>
    /// Does not have ImportSettings in meta, acts as ImportSettings on its own
    /// </summary>
    [YamlTagMapped]
    public abstract class NativeAssetData : AssetData
    {
        public abstract string FileExtension { get; }

        public static T CreateDefault<T>() where T : NativeAssetData
        {
            T assetData = Activator.CreateInstance<T>();
            assetData.SetDefaultValues();
            return assetData;
        }

        protected abstract void SetDefaultValues();
    }
}