using System.Collections.Generic;
using System.IO;
using System;
using LinearAlgebra;

namespace Engine.AssetsData
{
    [AssetData<Skeleton>]
    public class SkeletonData : AssetData
    {
        public List<Guid> AnimationData = new List<Guid>();
        public Matrix4x4f InverseRootTransform = Matrix4x4f.Identity;
        public List<BoneData> Bones = new List<BoneData>();

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

            skeleton.Animations = AnimationData;
            skeleton.Bones = Bones;
            skeleton.InverseRootTransform = InverseRootTransform;
            skeleton.GenerateGPUBuffer();

            return skeleton;
        }
    }

    public class BoneData
    {
        public string Name;
        public Matrix4x4f Transform;
        public Matrix4x4f Offset;

        public int Index;
        public int ParentIndex;
        public List<int> ChildIndices = new List<int>();
    }
}