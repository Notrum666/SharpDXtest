using System;
using System.IO;

namespace Engine.AssetsData
{
    public abstract class AssetData
    {
        public abstract void Serialize(BinaryWriter writer);
        public abstract void Deserialize(BinaryReader reader);

        public virtual BaseAsset ToRealAsset(BaseAsset targetAsset = null)
        {
            return null;
        }

        public BaseAsset ToRealAsset(Guid guid, BaseAsset targetAsset = null)
        {
            return ToRealAsset(targetAsset)?.WithGuid(guid);
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class AssetDataAttribute<T> : Attribute where T : BaseAsset { }
}