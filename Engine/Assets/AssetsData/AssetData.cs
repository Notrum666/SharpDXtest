using System;
using System.IO;

namespace Engine.AssetsData
{
    public class AssetData
    {
        public virtual void Serialize(BinaryWriter writer) { }
        public virtual void Deserialize(BinaryReader reader) { }

        public virtual object ToRealAsset(Type assetType) => null;
    }
}