using System;
using System.IO;

namespace Engine.AssetsData
{
    public class AssetData
    {
        public virtual void Serialize(BinaryWriter writer) { }
        public virtual void Deserialize(BinaryReader reader) { }

        public virtual BaseAsset ToRealAsset()
        {
            return null;
        }

        public BaseAsset ToRealAsset(Guid guid)
        {
            return ToRealAsset()?.WithGuid(guid);
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class AssetDataAttribute<T> : Attribute where T : BaseAsset { }
}