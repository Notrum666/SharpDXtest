using System.Collections.Generic;
using System.IO;
using System;

using LinearAlgebra;

namespace Engine.AssetsData
{
    [AssetData<Skeleton>]
    public class SkeletonData : NativeAssetData
    {
        public sealed override string FileExtension => ".sklt";

        // TODO: Add total bones count for inspector
        public Matrix4x4f InverseRootTransform = Matrix4x4f.Identity;
        public readonly List<Bone> Bones = new List<Bone>();

        protected sealed override void SetDefaultValues()
        {
            throw new NotImplementedException();
        }

        public sealed override void Serialize(BinaryWriter writer)
        {
            YamlManager.SaveToStream(writer.BaseStream, this);
        }

        public sealed override void Deserialize(BinaryReader reader)
        {
            YamlManager.LoadFromStream(reader.BaseStream, this);
        }

        public override Skeleton ToRealAsset(BaseAsset targetAsset = null)
        {
            Skeleton skeleton = targetAsset as Skeleton ?? new Skeleton();

            skeleton.Bones = Bones;
            skeleton.InverseRootTransform = InverseRootTransform;

            return skeleton;
        }
    }
}