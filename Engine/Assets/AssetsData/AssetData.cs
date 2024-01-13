using System;
using System.IO;

namespace Engine.AssetsData
{
    public abstract class AssetData
    {
        public abstract void Serialize(BinaryWriter writer);
        public abstract void Deserialize(BinaryReader reader);

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