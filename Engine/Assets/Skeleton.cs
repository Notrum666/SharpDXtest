using Engine.AssetsData;
using LinearAlgebra;
using SharpDX.Direct3D11;
using System.Collections.Generic;
using System;
using SharpDX;
using Assimp;

namespace Engine
{
    public class Skeleton : BaseAsset
    {
        public Matrix4x4f InverseRootTransform = Matrix4x4f.Identity;
        public List<Bone> Bones = new List<Bone>();
    }

    public class Bone
    {
        public string Name;
        public Matrix4x4f Offset = Matrix4x4f.Identity;

        public int Index;
        public int ParentIndex;
        public List<int> ChildIndices = new List<int>();
    }
}
