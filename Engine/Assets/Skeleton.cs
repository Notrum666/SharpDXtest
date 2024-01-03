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
        private bool disposed;

        public Matrix4x4f InverseRootTransform = Matrix4x4f.Identity;
        public List<BoneData> Bones = new List<BoneData>();
        public List<Guid> Animations = new List<Guid>();
        public int AnimationIndex = -1;
        private List<Matrix4x4f> BonesTransformations = new List<Matrix4x4f>();

        private int BonesTransformationsCount = 0;
        private SharpDX.Direct3D11.Buffer bonesBuffer = null;
        private ShaderResourceView bonesResourceView = null;

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                if (bonesBuffer is not null)
                    bonesBuffer.Dispose();
                if (bonesResourceView is not null)
                    bonesResourceView.Dispose();
            }
            disposed = true;

            base.Dispose(disposing);
        }

        public void GenerateGPUBuffer()
        {
            UpdateBoneTransformMatrices();
            if (BonesTransformations.Count > 0)
            {
                BonesTransformationsCount = BonesTransformations.Count;
                int matrixSize = Utilities.SizeOf<Matrix4x4f>();
                bonesBuffer = SharpDX.Direct3D11.Buffer.Create(GraphicsCore.CurrentDevice, BindFlags.ShaderResource,
                                            BonesTransformations.ToArray(), matrixSize * BonesTransformationsCount,
                                            ResourceUsage.Dynamic, CpuAccessFlags.Write,
                                            ResourceOptionFlags.BufferStructured, matrixSize);
                bonesResourceView = new ShaderResourceView(GraphicsCore.CurrentDevice, bonesBuffer, new ShaderResourceViewDescription()
                {
                    Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Buffer,
                    Format = SharpDX.DXGI.Format.Unknown,
                    Buffer = new ShaderResourceViewDescription.BufferResource()
                    {
                        FirstElement = 0,
                        ElementCount = BonesTransformationsCount
                    }
                });
            }
        }

        public void Use()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(Skeleton));

            if (bonesBuffer is not null && bonesResourceView is not null)
            {
                DataStream dataStream;
                GraphicsCore.CurrentDevice.ImmediateContext.MapSubresource(bonesBuffer, MapMode.WriteDiscard, MapFlags.None, out dataStream);
                for (int i = 0; i < BonesTransformationsCount; i++)
                    dataStream.Write(BonesTransformations[i]);
                GraphicsCore.CurrentDevice.ImmediateContext.UnmapSubresource(bonesBuffer, 0);
                GraphicsCore.CurrentDevice.ImmediateContext.VertexShader.SetShaderResource(0, bonesResourceView);
            }
        }

        private void UpdateBoneTransformMatrices()
        {
            if (Bones.Count > 0)
            {
                BonesTransformations.Clear();
                foreach (BoneData bone in Bones)
                    BonesTransformations.Add(bone.Transform);
            }
            if (BonesTransformationsCount != BonesTransformations.Count) // recreate gpu buffers with new size
            {
                if (bonesResourceView is not null)
                    bonesResourceView.Dispose();
                if (bonesBuffer is not null)
                    bonesBuffer.Dispose();
            }
        }

        private void UpdateTransform(Animation animation, float AnimationTime, BoneData boneData, Matrix4x4f parentTransform)
        {
            AnimationChannel curAnimation = null;
            foreach (AnimationChannel animationChannel in animation.Channels)
                if (animationChannel.BoneName == boneData.Name)
                {
                    curAnimation = animationChannel;
                    break;
                }
            Matrix4x4f nodeTransform = Matrix4x4f.Identity;
            if (curAnimation != null)
            {
                // get current keys
                // scaling
                Vector3f scaling;
                if (curAnimation.ScalingKeys.Count == 1)
                    scaling = curAnimation.ScalingKeys[0].Scaling;
                else
                {
                    int curIndex = 0;
                    for (int i = 0; i < curAnimation.ScalingKeys.Count - 1; ++i)
                        if (AnimationTime < curAnimation.ScalingKeys[i + 1].Time)
                        {
                            curIndex = i;
                            break;
                        }
                    int nextIndex = curIndex + 1;

                    float DeltaTime = curAnimation.ScalingKeys[nextIndex].Time - curAnimation.ScalingKeys[curIndex].Time;
                    float Factor = (AnimationTime - curAnimation.ScalingKeys[curIndex].Time) / DeltaTime;
                    Vector3f start = curAnimation.ScalingKeys[curIndex].Scaling;
                    Vector3f end = curAnimation.ScalingKeys[nextIndex].Scaling;
                    Vector3f delta = end - start;
                    scaling = start + Factor * delta;
                }

                // rotation
                LinearAlgebra.Quaternion rotation;
                if (curAnimation.RotationKeys.Count == 1)
                    rotation = curAnimation.RotationKeys[0].Rotation;
                else
                {
                    int curIndex = 0;
                    for (int i = 0; i < curAnimation.RotationKeys.Count - 1; ++i)
                        if (AnimationTime < curAnimation.RotationKeys[i + 1].Time)
                        {
                            curIndex = i;
                            break;
                        }
                    int nextIndex = curIndex + 1;

                    float DeltaTime = curAnimation.RotationKeys[nextIndex].Time - curAnimation.RotationKeys[curIndex].Time;
                    float Factor = (AnimationTime - curAnimation.RotationKeys[curIndex].Time) / DeltaTime;
                    LinearAlgebra.Quaternion start = curAnimation.RotationKeys[curIndex].Rotation;
                    LinearAlgebra.Quaternion end = curAnimation.RotationKeys[nextIndex].Rotation;
                    rotation = LinearAlgebra.Quaternion.Slerp(start, end, Factor);
                    rotation.normalize();
                }

                // position
                Vector3f position;
                if (curAnimation.PositionKeys.Count == 1)
                    position = curAnimation.PositionKeys[0].Position;
                else
                {
                    int curIndex = 0;
                    for (int i = 0; i < curAnimation.PositionKeys.Count - 1; ++i)
                        if (AnimationTime < curAnimation.PositionKeys[i + 1].Time)
                        {
                            curIndex = i;
                            break;
                        }
                    int nextIndex = curIndex + 1;

                    float DeltaTime = curAnimation.PositionKeys[nextIndex].Time - curAnimation.PositionKeys[curIndex].Time;
                    float Factor = (AnimationTime - curAnimation.PositionKeys[curIndex].Time) / DeltaTime;
                    Vector3f start = curAnimation.PositionKeys[curIndex].Position;
                    Vector3f end = curAnimation.PositionKeys[nextIndex].Position;
                    Vector3f delta = end - start;
                    position = start + Factor * delta;
                }

                nodeTransform = Matrix4x4f.FromTranslation(position) * Matrix4x4f.FromQuaternion(rotation) * Matrix4x4f.FromScale(scaling);
            }

            Matrix4x4f globalTransform = parentTransform * nodeTransform;
            boneData.Transform = InverseRootTransform * globalTransform * boneData.Offset;

            foreach (int childIndex in boneData.ChildIndices)
                UpdateTransform(animation, AnimationTime, Bones[childIndex], globalTransform);
        }

        public void UpdateAnimation()
        {
            // calculate transform matrices
            if (AnimationIndex >= 0 && Bones.Count > 0)
            {
                Animation curAnimation = AssetsManager.LoadAssetByGuid<Animation>(Animations[AnimationIndex]);
                float TicksPerSecond = (float)(curAnimation.TickPerSecond != 0 ? curAnimation.TickPerSecond : 25.0f);
                float TimeInTicks = (float)Time.TotalTime * TicksPerSecond;
                float AnimationTime = TimeInTicks % curAnimation.DurationInTicks;

                UpdateTransform(curAnimation, AnimationTime, Bones[0], Matrix4x4f.Identity);
            }
            else
                foreach (BoneData boneData in Bones)
                    boneData.Transform = Matrix4x4f.Identity;

            UpdateBoneTransformMatrices();
        }
    }
}
