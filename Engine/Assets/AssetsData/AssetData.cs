using System;
using System.IO;

namespace Engine.AssetsData
{
    public class AssetData
    {
        public virtual void Serialize(BinaryWriter writer) { }
        public virtual void Deserialize(BinaryReader reader) { }

        public virtual BaseAsset ToRealAsset(Type assetType)
        {
            return null;
        }

        public BaseAsset ToRealAsset(Type assetType, Guid guid)
        {
            return ToRealAsset(assetType)?.WithGuid(guid);
        }
    }
}