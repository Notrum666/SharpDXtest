using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LinearAlgebra;

namespace Engine.BaseAssets.Components
{
    public class Transform : Component
    {
        public Transform Parent { get; private set; }
        private Vector3 localPosition;
        public Vector3 LocalPosition
        {
            get
            {
                return localPosition;
            }
            set
            {
                localPosition = value;
                InvalidateMatrixes();
            }
        }
        public Vector3 Position 
        { 
            get 
            {
                return Parent == null ? LocalPosition : (Parent.Model * new Vector4(LocalPosition, 1.0)).xyz;
            } 
            set
            {
                if (Parent == null)
                    LocalPosition = value;
                else
                    LocalPosition = (Parent.View * new Vector4(value, 1.0)).xyz;
            }
        }
        private Quaternion localRotation;
        public Quaternion LocalRotation
        {
            get
            {
                return localRotation;
            }
            set
            {
                localRotation = value;
                InvalidateMatrixes();
            }
        }
        public Quaternion Rotation
        {
            get
            {
                return Parent == null ? LocalRotation : Parent.Rotation * LocalRotation;
            }
            set
            {
                if (Parent == null)
                    LocalRotation = value;
                else
                    LocalRotation = Parent.Rotation.inverse() * value;
            }
        }
        private Vector3 localScale;
        public Vector3 LocalScale
        {
            get
            {
                return localScale;
            }
            set
            {
                if (Math.Abs(value.x) <= Constants.Epsilon ||
                    Math.Abs(value.y) <= Constants.Epsilon ||
                    Math.Abs(value.z) <= Constants.Epsilon)
                    throw new ArgumentOutOfRangeException(nameof(LocalScale), "Scale can't be zero in any direction.");
                localScale = value;
                InvalidateMatrixes();
            }
        }

        private Matrix4x4 localModel;
        public Matrix4x4 LocalModel
        {
            get
            {
                if (matrixesRequireRecalculation)
                    RecalculateMatrixes();
                return localModel;
            }
        }
        public Matrix4x4 Model
        {
            get
            {
                return Parent == null ? LocalModel : Parent.Model * LocalModel;
            }
        }
        private Matrix4x4 localView;
        public Matrix4x4 LocalView
        {
            get
            {
                if (matrixesRequireRecalculation)
                    RecalculateMatrixes();
                return localView;
            }
        }
        public Matrix4x4 View
        {
            get
            {
                return Parent == null ? LocalView : LocalView * Parent.View;
            }
        }
        public Vector3 LocalForward { get { return LocalRotation * Vector3.Forward; } }
        public Vector3 LocalRight { get { return LocalRotation * Vector3.Right; } }
        public Vector3 LocalUp { get { return LocalRotation * Vector3.Up; } }
        public Vector3 Forward { get { return Rotation * Vector3.Forward; } }
        public Vector3 Right { get { return Rotation * Vector3.Right; } }
        public Vector3 Up { get { return Rotation * Vector3.Up; } }
        private bool matrixesRequireRecalculation;
        public void InvalidateMatrixes()
        {
            matrixesRequireRecalculation = true;
        }
        public void RecalculateMatrixes()
        {
            // Model * vec => Move * Rotate * Scale * vec
            localModel = Matrix4x4.FromQuaternion(LocalRotation);

            localModel.v00 *= localScale.x; localModel.v01 *= localScale.y; localModel.v02 *= localScale.z;
            localModel.v10 *= localScale.x; localModel.v11 *= localScale.y; localModel.v12 *= localScale.z;
            localModel.v20 *= localScale.x; localModel.v21 *= localScale.y; localModel.v22 *= localScale.z;

            localModel.v03 = LocalPosition.x;
            localModel.v13 = LocalPosition.y;
            localModel.v23 = LocalPosition.z;

            // View * vec => Scale^(-1) * Rotate^(-1) * Move^(-1) * vec
            localView = Matrix4x4.FromQuaternion(LocalRotation).transposed();

            double invScaleX = 1.0 / localScale.x;
            double invScaleY = 1.0 / localScale.y;
            double invScaleZ = 1.0 / localScale.z;
            localView.v00 *= invScaleX; localView.v01 *= invScaleX; localView.v02 *= invScaleX;
            localView.v10 *= invScaleY; localView.v11 *= invScaleY; localView.v12 *= invScaleY;
            localView.v20 *= invScaleZ; localView.v21 *= invScaleZ; localView.v22 *= invScaleZ;

            localView.v03 = -LocalPosition.x * localView.v00 - LocalPosition.y * localView.v01 - LocalPosition.z * localView.v02;
            localView.v13 = -LocalPosition.x * localView.v10 - LocalPosition.y * localView.v11 - LocalPosition.z * localView.v12;
            localView.v23 = -LocalPosition.x * localView.v20 - LocalPosition.y * localView.v21 - LocalPosition.z * localView.v22;

            matrixesRequireRecalculation = false;
        }
        public Transform()
        {
            LocalPosition = Vector3.Zero;
            LocalRotation = Quaternion.Identity;
            localScale = new Vector3(1.0, 1.0, 1.0);

            RecalculateMatrixes();
        }
        public void setParent(Transform transform)
        {
            Parent = transform;
        }
    }
}
