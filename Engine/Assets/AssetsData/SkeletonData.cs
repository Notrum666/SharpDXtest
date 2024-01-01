using System.Collections.Generic;
using System.IO;
using LinearAlgebra;

namespace Engine.AssetsData
{
    [AssetData<BaseAsset>]
    public class SkeletonData : AssetData
    {
        //TODO: InverseRootTransform
        public List<BoneData> Bones = new List<BoneData>();

        public override void Serialize(BinaryWriter writer)
        {
            YamlManager.SaveToStream(writer.BaseStream, this);
        }

        public override void Deserialize(BinaryReader reader)
        {
            YamlManager.LoadFromStream(reader.BaseStream, this);
        }

        public override BaseAsset ToRealAsset()
        {
            BaseAsset skeleton = new BaseAsset();

            return skeleton;
        }
    }

    public class BoneData //Node
    {
        public string Name;
        public Matrix4x4f Transform;

        public int Index;
        public int ParentIndex;
        public List<int> ChildIndices = new List<int>();
    }
}