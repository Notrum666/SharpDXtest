using LinearAlgebra;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Engine.BaseAssets.Components
{
    public class SkeletalMeshComponent : MeshComponent
    {
        bool destroyed = false;

        [SerializedField]
        private Skeleton[] skeletons = Array.Empty<Skeleton>();
        [SerializedField]
        private int animationIndex = 0;
        [SerializedField]
        private float animationTime = 0;

        public Skeleton[] Skeletons => skeletons;

        public int AnimationIndex
        {
            get => animationIndex;
            set
            {
                animationIndex = value;
                animationTime = 0;
            }
        }

        public new Model Model
        {
            get => Model;
            set
            {
                model = value;
                RefreshMaterialsSlots();
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

        private List<Matrix4x4f> BonesTransformations = new List<Matrix4x4f>();
        private List<Matrix4x4f> InverseTransposeBonesTransformations = new List<Matrix4x4f>();

        private int BonesTransformationsCount = 0;
        private SharpDX.Direct3D11.Buffer bonesBuffer = null;
        private ShaderResourceView bonesResourceView = null;
        private SharpDX.Direct3D11.Buffer inverseTransposeBonesBuffer = null;
        private ShaderResourceView inverseTransposeBonesResourceView = null;

        protected override void OnDestroy()
        {
            if (bonesBuffer is not null)
                bonesBuffer.Dispose();
            if (bonesResourceView is not null)
                bonesResourceView.Dispose();
            if (inverseTransposeBonesBuffer is not null)
                inverseTransposeBonesBuffer.Dispose();
            if (inverseTransposeBonesResourceView is not null)
                inverseTransposeBonesResourceView.Dispose();

            destroyed = true;
        }

        public void EnsureGPUBuffer()
        {
            CheckBonesTransformationsCount();
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
                inverseTransposeBonesBuffer = SharpDX.Direct3D11.Buffer.Create(GraphicsCore.CurrentDevice, BindFlags.ShaderResource,
                                            InverseTransposeBonesTransformations.ToArray(), matrixSize * BonesTransformationsCount,
                                            ResourceUsage.Dynamic, CpuAccessFlags.Write,
                                            ResourceOptionFlags.BufferStructured, matrixSize);
                inverseTransposeBonesResourceView = new ShaderResourceView(GraphicsCore.CurrentDevice, inverseTransposeBonesBuffer, new ShaderResourceViewDescription()
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
            if (destroyed)
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
            if (inverseTransposeBonesBuffer is not null && inverseTransposeBonesResourceView is not null)
            {
                DataStream dataStream;
                GraphicsCore.CurrentDevice.ImmediateContext.MapSubresource(inverseTransposeBonesBuffer, MapMode.WriteDiscard, MapFlags.None, out dataStream);
                for (int i = 0; i < BonesTransformationsCount; i++)
                    dataStream.Write(InverseTransposeBonesTransformations[i]);
                GraphicsCore.CurrentDevice.ImmediateContext.UnmapSubresource(inverseTransposeBonesBuffer, 0);
                GraphicsCore.CurrentDevice.ImmediateContext.VertexShader.SetShaderResource(1, inverseTransposeBonesResourceView);
            }
        }

        private void CheckBonesTransformationsCount()
        {
            if (BonesTransformationsCount != BonesTransformations.Count) // recreate gpu buffers with new size
            {
                if (bonesResourceView is not null)
                    bonesResourceView.Dispose();
                if (bonesBuffer is not null)
                    bonesBuffer.Dispose();
                if (inverseTransposeBonesBuffer is not null)
                    inverseTransposeBonesBuffer.Dispose();
                if (inverseTransposeBonesResourceView is not null)
                    inverseTransposeBonesResourceView.Dispose();
            }
        }

        private void UpdateTransform(Skeleton skeleton, Animation animation, float AnimationTime, Bone boneData, Matrix4x4f parentTransform)
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
            BonesTransformations[boneData.Index] = skeleton.InverseRootTransform * globalTransform * boneData.Offset;
            InverseTransposeBonesTransformations[boneData.Index] = BonesTransformations[boneData.Index].inverse().transposed();

            foreach (int childIndex in boneData.ChildIndices)
                UpdateTransform(skeleton, animation, AnimationTime, skeleton.Bones[childIndex], globalTransform);
        }

        public void UpdateAnimation()
        {
            // calculate transform matrices
            foreach (Skeleton skeleton in skeletons) {
                if (animationIndex >= 0 && skeleton.Bones.Count > 0)
                {
                    BonesTransformations.Clear();
                    InverseTransposeBonesTransformations.Clear();
                    BonesTransformations.AddRange(skeleton.Bones.Select(b => b.Offset)); // fill BonesTransformations and InverseTransposeBonesTransformations somehow just to resize list to skeleton.Bones.Count
                    InverseTransposeBonesTransformations.AddRange(skeleton.Bones.Select(b => b.Offset));
                    animationTime += (float)Time.DeltaTime;
                    Animation curAnimation = AssetsManager.LoadAssetByGuid<Animation>(skeleton.Animations[animationIndex]);
                    float TicksPerSecond = (float)(curAnimation.TickPerSecond != 0 ? curAnimation.TickPerSecond : 25.0f);
                    float TimeInTicks = animationTime * TicksPerSecond;
                    float AnimationTime = TimeInTicks % curAnimation.DurationInTicks;

                    UpdateTransform(skeleton,curAnimation, AnimationTime, skeleton.Bones[0], Matrix4x4f.Identity);
                }
            }

            CheckBonesTransformationsCount();
        }

        public override void Render()
        {
            UpdateAnimation();
            EnsureGPUBuffer();
            Use();
            base.Render();
        }

        public override void OnFieldChanged(FieldInfo fieldInfo)
        {
            if (fieldInfo.Name == nameof(model)) {
                RefreshMaterialsSlots();
                RefreshSkeletonSlots();
            }
        }
    }
}
