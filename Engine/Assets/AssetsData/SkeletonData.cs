using System.Collections.Generic;
using System.IO;
using System;

using LinearAlgebra;

namespace Engine.AssetsData
{
    [AssetData<Skeleton>]
    public class SkeletonData : NativeAssetData
    {
        // TODO: Add total bones count for inspector
        public Matrix4x4f InverseRootTransform = Matrix4x4f.Identity;
        public readonly List<Bone> Bones = new List<Bone>();

        protected sealed override void SetDefaultValues()
        {
            throw new NotImplementedException();
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