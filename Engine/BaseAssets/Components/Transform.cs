using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using LinearAlgebra;

namespace Engine.BaseAssets.Components
{
    public class Transform : Component, INotifyFieldChanged
    {
        public event Action Invalidated;
        public Transform Parent { get; private set; }
        [DisplayField("Position")]
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
                invalidateCachedData();
            }
        }
        private Vector3 position;
        public Vector3 Position 
        { 
            get 
            {
                if (requiresCachedDataRecalculation)
                    recalculateCachedData();
                return position;
            } 
            set
            {
                if (Parent == null)
                    LocalPosition = value;
                else
                    LocalPosition = Parent.View.TransformPoint(value);
            }
        }
        [DisplayField("Rotation", typeof(QuaternionToEulerDegreesConverter), true)]
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
                invalidateCachedData();
            }
        }
        private Quaternion rotation;
        public Quaternion Rotation
        {
            get
            {
                if (requiresCachedDataRecalculation)
                    recalculateCachedData();
                return rotation;
            }
            set
            {
                if (Parent == null)
                {
                    LocalRotation = value;
                    return;
                }

                Quaternion rot;
                decomposeModel(Parent.View * createModel(Position, value, Scale), out _, out rot, out _);

                LocalRotation = rot;
            }
        }
        [DisplayField("Scale")]
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
                invalidateCachedData();
            }
        }
        private Vector3 scale;
        private Vector3 Scale
        {
            get
            {
                if (requiresCachedDataRecalculation)
                    recalculateCachedData();
                return scale;
            }
        }
        private Matrix4x4 localModel;
        public Matrix4x4 LocalModel
        {
            get
            {
                if (requiresCachedDataRecalculation)
                    recalculateCachedData();
                return localModel;
            }
        }
        private Matrix4x4 model;
        public Matrix4x4 Model
        {
            get
            {
                if (requiresCachedDataRecalculation)
                    recalculateCachedData();
                return model;
            }
        }
        private Matrix4x4 localView;
        public Matrix4x4 LocalView
        {
            get
            {
                if (requiresCachedDataRecalculation)
                    recalculateCachedData();
                return localView;
            }
        }
        private Matrix4x4 view;
        public Matrix4x4 View
        {
            get
            {
                if (requiresCachedDataRecalculation)
                    recalculateCachedData();
                return view;
            }
        }
        public Vector3 LocalForward { get { return LocalRotation * Vector3.Forward; } }
        public Vector3 LocalRight { get { return LocalRotation * Vector3.Right; } }
        public Vector3 LocalUp { get { return LocalRotation * Vector3.Up; } }
        public Vector3 Forward { get { return Rotation * Vector3.Forward; } }
        public Vector3 Right { get { return Rotation * Vector3.Right; } }
        public Vector3 Up { get { return Rotation * Vector3.Up; } }
        private bool requiresCachedDataRecalculation;
        private void invalidateCachedData()
        {
            Invalidated?.Invoke();
            requiresCachedDataRecalculation = true;
        }
        private void decomposeModel(in Matrix4x4 model, out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            position = new Vector3(model.v03, model.v13, model.v23);
        
            Matrix3x3 prevRot;
            Matrix3x3 rot = new Matrix3x3(model.v00, model.v01, model.v02,
                                          model.v10, model.v11, model.v12,
                                          model.v20, model.v21, model.v22);
            
            do
            {
                prevRot = rot;
        
                rot.invert();
                rot.transpose();
                rot = 0.5 * (prevRot + rot);
            } while (!(rot - prevRot).IsZero());

            rotation = Quaternion.FromMatrix(rot);

            rot.transpose();
            rot = rot * new Matrix3x3(model.v00, model.v01, model.v02,
                                      model.v10, model.v11, model.v12,
                                      model.v20, model.v21, model.v22);

            scale = new Vector3(rot.v00, rot.v11, rot.v22);
        }
        private Matrix4x4 createModel(in Vector3 position, in Quaternion rotation, in Vector3 scale)
        {
            // Model * vec => Move * Rotate * Scale * vec
            Matrix4x4 res = Matrix4x4.FromQuaternion(rotation);

            res.v00 *= scale.x; res.v01 *= scale.y; res.v02 *= scale.z;
            res.v10 *= scale.x; res.v11 *= scale.y; res.v12 *= scale.z;
            res.v20 *= scale.x; res.v21 *= scale.y; res.v22 *= scale.z;

            res.v03 = position.x;
            res.v13 = position.y;
            res.v23 = position.z;

            return res;
        }
        private Matrix4x4 createView(in Vector3 position, in Quaternion rotation, in Vector3 scale)
        {
            // View * vec => Scale^(-1) * Rotate^(-1) * Move^(-1) * vec
            Matrix4x4 res = Matrix4x4.FromQuaternion(rotation);
            res.transpose();

            double invScaleX = 1.0 / scale.x;
            double invScaleY = 1.0 / scale.y;
            double invScaleZ = 1.0 / scale.z;
            res.v00 *= invScaleX; res.v01 *= invScaleX; res.v02 *= invScaleX;
            res.v10 *= invScaleY; res.v11 *= invScaleY; res.v12 *= invScaleY;
            res.v20 *= invScaleZ; res.v21 *= invScaleZ; res.v22 *= invScaleZ;

            res.v03 = -position.x * res.v00 - position.y * res.v01 - position.z * res.v02;
            res.v13 = -position.x * res.v10 - position.y * res.v11 - position.z * res.v12;
            res.v23 = -position.x * res.v20 - position.y * res.v21 - position.z * res.v22;

            return res;
        }
        private void recalculateCachedData()
        {
            localModel = createModel(localPosition, localRotation, localScale);
            model = Parent == null ? localModel : Parent.Model * localModel;

            localView = createView(localPosition, localRotation, localScale);
            view = Parent == null ? localView : localView * Parent.View;

            decomposeModel(model, out position, out rotation, out scale);

            requiresCachedDataRecalculation = false;
        }
        public Transform()
        {
            localPosition = Vector3.Zero;
            localRotation = Quaternion.Identity;
            localScale = new Vector3(1.0, 1.0, 1.0);

            recalculateCachedData();
        }
        public void SetParent(Transform transform, bool keepRelative = true)
        {
            if (Parent != null)
                Parent.Invalidated -= invalidateCachedData;

            Transform tmp = transform;
            while (tmp != null)
            {
                if (tmp == this)
                    throw new ArgumentException("Object can't be ancestor of itself in transform hierarchy.");
                tmp = tmp.Parent;
            }

            if (keepRelative)
            {
                Matrix4x4 model = transform == null ? Model : transform.View * Model;
                Parent = transform;
                decomposeModel(model, out localPosition, out localRotation, out localScale);
            }
            else
                Parent = transform;

            invalidateCachedData();

            if (Parent != null)
                Parent.Invalidated += invalidateCachedData;
        }

        public void OnFieldChanged(FieldInfo fieldInfo)
        {
            invalidateCachedData();
        }
    }
}
