using LinearAlgebra;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using Buffer = SharpDX.Direct3D11.Buffer;

namespace Engine.BaseAssets.Components
{
    public class SkeletalMeshComponent : MeshComponent, IDisposable
    {
        [SerializedField]
        private Skeleton[] skeletons = Array.Empty<Skeleton>();
        [SerializeField]
        private SkeletalAnimation animation = null;
        [SerializedField]
        private float animationTime = 0;

        public Skeleton[] Skeletons => skeletons;

        // private List<Matrix4x4f> BonesTransformations = new List<Matrix4x4f>();
        // private List<Matrix4x4f> InverseTransposeBonesTransformations = new List<Matrix4x4f>();

        private int currentBufferElementsCount = 0;
        private Buffer bonesBuffer = null;
        private ShaderResourceView bonesResourceView = null;
        private Buffer inverseTransposeBonesBuffer = null;
        private ShaderResourceView inverseTransposeBonesResourceView = null;
        private bool disposed = false;

        public SkeletalAnimation Animation
        {
            get => Animation;
            set
            {
                animation = value;
                animationTime = 0;
            }
        }

        public override Model Model
        {
            get => Model;
            set
            {
                base.Model = value;
                RefreshSkeletonSlots();
            }
        }

        protected void RefreshSkeletonSlots()
        {
            if (model is null)
                skeletons = Array.Empty<Skeleton>();
            else
                skeletons = model.Meshes.Select(p => p.Skeleton).ToArray();
        }

        public override void Render()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(SkeletalMeshComponent));

            List<Matrix4x4f> BonesTransform;
            List<Matrix4x4f> InvTrspsBonesTransform;
            UpdateAnimation(out BonesTransform, out InvTrspsBonesTransform);
            EnsureGPUBuffer(BonesTransform, InvTrspsBonesTransform);
            Use(BonesTransform, InvTrspsBonesTransform);
            base.Render();
        }

        public override void OnFieldChanged(FieldInfo fieldInfo)
        {
            base.OnFieldChanged(fieldInfo);
            if (fieldInfo.Name == nameof(model))
                RefreshSkeletonSlots();
        }

        private void EnsureGPUBuffer(List<Matrix4x4f> BonesTransformations, List<Matrix4x4f> InverseTransposeBonesTransformations)
        {
            if (BonesTransformations.Count > 0 && (bonesBuffer is null || BonesTransformations.Count != currentBufferElementsCount))
            {
                ClearBuffers();
                currentBufferElementsCount = BonesTransformations.Count;
                int matrixSize = Marshal.SizeOf<Matrix4x4f>();

                bonesBuffer = Buffer.Create(GraphicsCore.CurrentDevice, BindFlags.ShaderResource,
                                            BonesTransformations.ToArray(), matrixSize * currentBufferElementsCount,
                                            ResourceUsage.Dynamic, CpuAccessFlags.Write,
                                            ResourceOptionFlags.BufferStructured, matrixSize);
                bonesResourceView = new ShaderResourceView(GraphicsCore.CurrentDevice, bonesBuffer, new ShaderResourceViewDescription()
                {
                    Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Buffer,
                    Format = SharpDX.DXGI.Format.Unknown,
                    Buffer = new ShaderResourceViewDescription.BufferResource()
                    {
                        FirstElement = 0,
                        ElementCount = currentBufferElementsCount
                    }
                });

                inverseTransposeBonesBuffer = Buffer.Create(GraphicsCore.CurrentDevice, BindFlags.ShaderResource,
                                            InverseTransposeBonesTransformations.ToArray(), matrixSize * currentBufferElementsCount,
                                            ResourceUsage.Dynamic, CpuAccessFlags.Write,
                                            ResourceOptionFlags.BufferStructured, matrixSize);
                inverseTransposeBonesResourceView = new ShaderResourceView(GraphicsCore.CurrentDevice, inverseTransposeBonesBuffer, new ShaderResourceViewDescription()
                {
                    Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Buffer,
                    Format = SharpDX.DXGI.Format.Unknown,
                    Buffer = new ShaderResourceViewDescription.BufferResource()
                    {
                        FirstElement = 0,
                        ElementCount = currentBufferElementsCount
                    }
                });
            }
        }

        private void Use(List<Matrix4x4f> BonesTransformations, List<Matrix4x4f> InverseTransposeBonesTransformations)
        {
            if (bonesBuffer is not null && bonesResourceView is not null)
            {
                DataStream dataStream;
                GraphicsCore.CurrentDevice.ImmediateContext.MapSubresource(bonesBuffer, MapMode.WriteDiscard, MapFlags.None, out dataStream);
                for (int i = 0; i < currentBufferElementsCount; i++)
                    dataStream.Write(BonesTransformations[i]);
                GraphicsCore.CurrentDevice.ImmediateContext.UnmapSubresource(bonesBuffer, 0);
                GraphicsCore.CurrentDevice.ImmediateContext.VertexShader.SetShaderResource(0, bonesResourceView);
            }
            if (inverseTransposeBonesBuffer is not null && inverseTransposeBonesResourceView is not null)
            {
                DataStream dataStream;
                GraphicsCore.CurrentDevice.ImmediateContext.MapSubresource(inverseTransposeBonesBuffer, MapMode.WriteDiscard, MapFlags.None, out dataStream);
                for (int i = 0; i < currentBufferElementsCount; i++)
                    dataStream.Write(InverseTransposeBonesTransformations[i]);
                GraphicsCore.CurrentDevice.ImmediateContext.UnmapSubresource(inverseTransposeBonesBuffer, 0);
                GraphicsCore.CurrentDevice.ImmediateContext.VertexShader.SetShaderResource(1, inverseTransposeBonesResourceView);
            }
        }

        private void ClearBuffers()
        {
            if (bonesBuffer is null)
                return;

            bonesBuffer.Dispose();
            bonesResourceView.Dispose();
            inverseTransposeBonesBuffer.Dispose();
            inverseTransposeBonesResourceView.Dispose();
            bonesBuffer = null;
            bonesResourceView = null;
            inverseTransposeBonesBuffer = null;
            inverseTransposeBonesResourceView = null;
        }

        private void UpdateTransform(Skeleton skeleton, float AnimationTime, Bone boneData, Matrix4x4f parentTransform, List<Matrix4x4f> BonesTransform, List<Matrix4x4f> InvTrspsBonesTransform)
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
            BonesTransform[boneData.Index] = skeleton.InverseRootTransform * globalTransform * boneData.Offset;
            InvTrspsBonesTransform[boneData.Index] = BonesTransform[boneData.Index].inverse().transposed();

            foreach (int childIndex in boneData.ChildIndices)
                UpdateTransform(skeleton, AnimationTime, skeleton.Bones[childIndex], globalTransform, BonesTransform, InvTrspsBonesTransform);
        }

        private void UpdateAnimation(out List<Matrix4x4f> BonesTransformations, out List<Matrix4x4f> InverseTransposeBonesTransformations)
        {
            BonesTransformations = new List<Matrix4x4f>();
            InverseTransposeBonesTransformations = new List<Matrix4x4f>();
            // calculate transform matrices
            foreach (Skeleton skeleton in skeletons) {
                if (skeleton.Bones.Count > 0)
                {
                    BonesTransformations = skeleton.Bones.Select(_ => Matrix4x4f.Identity).ToList();
                    InverseTransposeBonesTransformations = new List<Matrix4x4f>(BonesTransformations);
                    if (animation is not null) {
                        animationTime += (float)Time.DeltaTime;
                        float TicksPerSecond = (float)(animation.TickPerSecond != 0 ? animation.TickPerSecond : 25.0f);
                        float TimeInTicks = animationTime * TicksPerSecond;
                        float AnimationTime = TimeInTicks % animation.DurationInTicks;

                        UpdateTransform(skeleton, AnimationTime, skeleton.Bones[0], Matrix4x4f.Identity, BonesTransformations, InverseTransposeBonesTransformations);
                    }
                    return;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                ClearBuffers();

                disposed = true;
            }
        }

        ~SkeletalMeshComponent()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
