using System.Collections.Generic;
using System.IO;
using System;
using LinearAlgebra;

namespace Engine.AssetsData
{
    [AssetData<Skeleton>]
    public class SkeletonData : AssetData
    {
        public Matrix4x4f InverseRootTransform = Matrix4x4f.Identity;
        public List<Bone> Bones = new List<Bone>();

        public override void Serialize(BinaryWriter writer)
        {
            YamlManager.SaveToStream(writer.BaseStream, this);
        }

        public override void Deserialize(BinaryReader reader)
        {
            YamlManager.LoadFromStream(reader.BaseStream, this);
        }

        public override Skeleton ToRealAsset()
        {
            Skeleton skeleton = new Skeleton();

            skeleton.Bones = Bones;
            skeleton.InverseRootTransform = InverseRootTransform;

            return skeleton;
        }
    }
}