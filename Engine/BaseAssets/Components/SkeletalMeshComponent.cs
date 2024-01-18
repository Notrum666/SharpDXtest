using LinearAlgebra;

using SharpDX;
using SharpDX.Direct3D11;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using Buffer = SharpDX.Direct3D11.Buffer;

namespace Engine.BaseAssets.Components
{
    public class SkeletalMeshComponent : MeshComponent, IDisposable
    {
        [SerializedField]
        private Skeleton skeleton;
        [SerializedField]
        private SkeletalAnimation animation = null;
        [SerializedField]
        private float animationCurrentTime = 0;

        public Skeleton Skeleton => skeleton;

        // private List<Matrix4x4f> BonesTransformations = new List<Matrix4x4f>();
        // private List<Matrix4x4f> InverseTransposeBonesTransformations = new List<Matrix4x4f>();
        private List<Matrix4x4f> bonesTransforms = null;
        public ReadOnlyCollection<Matrix4x4f> BonesTransforms => bonesTransforms.AsReadOnly();

        private int currentBufferElementsCount = 0;
        private Buffer bonesBuffer = null;
        private Buffer inverseTransposeBonesBuffer = null;

        private ShaderResourceView bonesResourceView = null;
        private ShaderResourceView inverseTransposeBonesResourceView = null;

        private bool disposed = false;

        public SkeletalAnimation Animation
        {
            get => animation;
            set
            {
                animation = value;
                animationCurrentTime = 0;
            }
        }

        public override Model Model
        {
            get => base.Model;
            set
            {
                base.Model = value;
                RefreshSkeletonSlots();
            }
        }

        protected void RefreshSkeletonSlots()
        {
            skeleton = model?.Skeleton;
        }

        public override void Render(bool withMaterials = true)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(SkeletalMeshComponent));

            UpdateAnimation(out List<Matrix4x4f> curBonesTransforms, out List<Matrix4x4f> invTransposeBonesTransform);
            bonesTransforms = curBonesTransforms;
            EnsureGPUBuffer(curBonesTransforms, invTransposeBonesTransform);
            Use(curBonesTransforms, invTransposeBonesTransform);
            base.Render(withMaterials);
            GraphicsCore.CurrentDevice.ImmediateContext.VertexShader.SetShaderResource(0, null);
            GraphicsCore.CurrentDevice.ImmediateContext.VertexShader.SetShaderResource(1, null);
        }

        public override void OnFieldChanged(FieldInfo fieldInfo)
        {
            base.OnFieldChanged(fieldInfo);
            if (fieldInfo.Name == nameof(model))
                RefreshSkeletonSlots();
        }

        private void EnsureGPUBuffer(List<Matrix4x4f> bonesTransformations, List<Matrix4x4f> inverseTransposeBonesTransformations)
        {
            if (bonesTransformations.Count > 0 && (bonesBuffer is null || bonesTransformations.Count != currentBufferElementsCount))
            {
                ClearBuffers();
                currentBufferElementsCount = bonesTransformations.Count;
                int matrixSize = Marshal.SizeOf<Matrix4x4f>();

                bonesBuffer = Buffer.Create(GraphicsCore.CurrentDevice, BindFlags.ShaderResource,
                                            bonesTransformations.ToArray(), matrixSize * currentBufferElementsCount,
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
                                                            inverseTransposeBonesTransformations.ToArray(), matrixSize * currentBufferElementsCount,
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

        private void Use(List<Matrix4x4f> bonesTransformations, List<Matrix4x4f> inverseTransposeBonesTransformations)
        {
            if (bonesBuffer is not null && bonesResourceView is not null)
            {
                DataStream dataStream;
                GraphicsCore.CurrentDevice.ImmediateContext.MapSubresource(bonesBuffer, MapMode.WriteDiscard, MapFlags.None, out dataStream);
                for (int i = 0; i < currentBufferElementsCount; i++)
                    dataStream.Write(bonesTransformations[i]);
                GraphicsCore.CurrentDevice.ImmediateContext.UnmapSubresource(bonesBuffer, 0);
                GraphicsCore.CurrentDevice.ImmediateContext.VertexShader.SetShaderResource(0, bonesResourceView);
            }
            if (inverseTransposeBonesBuffer is not null && inverseTransposeBonesResourceView is not null)
            {
                DataStream dataStream;
                GraphicsCore.CurrentDevice.ImmediateContext.MapSubresource(inverseTransposeBonesBuffer, MapMode.WriteDiscard, MapFlags.None, out dataStream);
                for (int i = 0; i < currentBufferElementsCount; i++)
                    dataStream.Write(inverseTransposeBonesTransformations[i]);
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

        private void UpdateTransform(float animationTime, Bone boneData, Matrix4x4f parentTransform, List<Matrix4x4f> bonesTransform, List<Matrix4x4f> invTrspsBonesTransform)
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
                        if (animationTime < curAnimation.ScalingKeys[i + 1].Time)
                        {
                            curIndex = i;
                            break;
                        }
                    int nextIndex = curIndex + 1;

                    float deltaTime = curAnimation.ScalingKeys[nextIndex].Time - curAnimation.ScalingKeys[curIndex].Time;
                    float factor = (animationTime - curAnimation.ScalingKeys[curIndex].Time) / deltaTime;
                    Vector3f start = curAnimation.ScalingKeys[curIndex].Scaling;
                    Vector3f end = curAnimation.ScalingKeys[nextIndex].Scaling;
                    Vector3f delta = end - start;
                    scaling = start + factor * delta;
                }

                // rotation
                LinearAlgebra.Quaternion rotation;
                if (curAnimation.RotationKeys.Count == 1)
                    rotation = curAnimation.RotationKeys[0].Rotation;
                else
                {
                    int curIndex = 0;
                    for (int i = 0; i < curAnimation.RotationKeys.Count - 1; ++i)
                        if (animationTime < curAnimation.RotationKeys[i + 1].Time)
                        {
                            curIndex = i;
                            break;
                        }
                    int nextIndex = curIndex + 1;

                    float deltaTime = curAnimation.RotationKeys[nextIndex].Time - curAnimation.RotationKeys[curIndex].Time;
                    float factor = (animationTime - curAnimation.RotationKeys[curIndex].Time) / deltaTime;
                    LinearAlgebra.Quaternion start = curAnimation.RotationKeys[curIndex].Rotation;
                    LinearAlgebra.Quaternion end = curAnimation.RotationKeys[nextIndex].Rotation;
                    rotation = LinearAlgebra.Quaternion.Slerp(start, end, factor);
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
                        if (animationTime < curAnimation.PositionKeys[i + 1].Time)
                        {
                            curIndex = i;
                            break;
                        }
                    int nextIndex = curIndex + 1;

                    float deltaTime = curAnimation.PositionKeys[nextIndex].Time - curAnimation.PositionKeys[curIndex].Time;
                    float factor = (animationTime - curAnimation.PositionKeys[curIndex].Time) / deltaTime;
                    Vector3f start = curAnimation.PositionKeys[curIndex].Position;
                    Vector3f end = curAnimation.PositionKeys[nextIndex].Position;
                    Vector3f delta = end - start;
                    position = start + factor * delta;
                }

                nodeTransform = Matrix4x4f.FromTranslation(position) * Matrix4x4f.FromQuaternion(rotation) * Matrix4x4f.FromScale(scaling);
            }

            Matrix4x4f globalTransform = parentTransform * nodeTransform;
            bonesTransform[boneData.Index] = skeleton.InverseRootTransform * globalTransform * boneData.Offset;
            invTrspsBonesTransform[boneData.Index] = bonesTransform[boneData.Index].inverse().transposed();

            foreach (int childIndex in boneData.ChildIndices)
                UpdateTransform(animationTime, skeleton.Bones[childIndex], globalTransform, bonesTransform, invTrspsBonesTransform);
        }

        private void UpdateAnimation(out List<Matrix4x4f> bonesTransformations, out List<Matrix4x4f> inverseTransposeBonesTransformations)
        {
            bonesTransformations = new List<Matrix4x4f>();
            inverseTransposeBonesTransformations = new List<Matrix4x4f>();
            // calculate transform matrices
            if (skeleton.Bones.Count > 0)
            {
                bonesTransformations = skeleton.Bones.Select(_ => Matrix4x4f.Identity).ToList();
                inverseTransposeBonesTransformations = new List<Matrix4x4f>(bonesTransformations);
                if (animation is not null)
                {
                    animationCurrentTime += EngineCore.IsPaused ? 0 : (float)Time.DeltaTime;
                    float ticksPerSecond = (float)(animation.TickPerSecond != 0 ? animation.TickPerSecond : 25.0f);
                    float timeInTicks = animationCurrentTime * ticksPerSecond;
                    float animationTime = timeInTicks % animation.DurationInTicks;

                    UpdateTransform(animationTime, skeleton.Bones[0], Matrix4x4f.Identity, bonesTransformations, inverseTransposeBonesTransformations);
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