using LinearAlgebra;
using System.Collections.Generic;

namespace Engine
{
    public class Skeleton : BaseAsset
    {
        public Matrix4x4f InverseRootTransform = Matrix4x4f.Identity;
        public List<Bone> Bones = new List<Bone>();
    }

    // TODO: split to two classes for Skeleton and SkeletonData
    public class Bone
    {
        public string Name;
        public Matrix4x4f Offset = Matrix4x4f.Identity;

        public int Index;
        public int ParentIndex;
        public List<int> ChildIndices = new List<int>();
    }
}
