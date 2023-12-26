using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Reflection;

using LinearAlgebra;

namespace Engine.BaseAssets.Components
{
    public partial class Transform : Component, INotifyFieldChanged
    {
        [SerializedField]
        private Transform parent = null;

        [SerializedField]
        private readonly List<Transform> children = new List<Transform>();

        public Transform Parent { get => parent; private set => parent = value; }
        public ReadOnlyCollection<Transform> Children => children.AsReadOnly();

        private event Action Invalidated;
        private bool requiresCachedDataRecalculation;

        /// <summary>
        /// Calls <see cref="GameObject.DestroyImmediateInternal()">GameObject.DestroyImmediate()</see> <br/>
        /// Calls <see cref="Component.DestroyImmediateInternal()">Component.DestroyImmediate()</see> <br/>
        /// Calls <see cref="Transform.DestroyImmediateInternal()">Transform.DestroyImmediate()</see> on all children <br/>
        /// Removes Parent(Transform) linking 
        /// </summary>
        private protected override void DestroyImmediateInternal()
        {
            GameObject.DestroyImmediate();

            base.DestroyImmediateInternal();

            foreach (Transform child in children.ToImmutableArray())
                child.DestroyImmediate();

            if (Parent != null)
            {
                Parent.children.Remove(this);
                Parent.Invalidated -= InvalidateCachedData;
                Parent = null;
            }
        }

        internal override void OnDeserialized()
        {
            requiresCachedDataRecalculation = true;

            if (Parent != null)
                Parent.Invalidated += InvalidateCachedData;
        }

        public void SetParent(Transform transform, bool keepRelative = true)
        {
            if (Parent != null)
                Parent.Invalidated -= InvalidateCachedData;

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
                DecomposeModel(model, out localPosition, out localRotation, out localScale);
            }
            Parent = transform;

            InvalidateCachedData();

            if (Parent != null)
            {
                Parent.Invalidated += InvalidateCachedData;
                Parent.children.Add(this);
            }
        }

        public void OnFieldChanged(FieldInfo fieldInfo)
        {
            InvalidateCachedData();
        }

        private void InvalidateCachedData()
        {
            Invalidated?.Invoke();
            requiresCachedDataRecalculation = true;
        }
    }

    public partial class Transform
    {
        [SerializedField]
        private Vector3 localPosition;
        [SerializedField]
        private Quaternion localRotation;
        [SerializedField]
        private Vector3 localScale;

        public Vector3 LocalPosition
        {
            get => localPosition;
            set
            {
                localPosition = value;
                InvalidateCachedData();
            }
        }

        public Quaternion LocalRotation
        {
            get => localRotation;
            set
            {
                localRotation = value;
                InvalidateCachedData();
            }
        }

        public Vector3 LocalScale
        {
            get => localScale;
            set
            {
                if (Math.Abs(value.x) <= Constants.Epsilon ||
                    Math.Abs(value.y) <= Constants.Epsilon ||
                    Math.Abs(value.z) <= Constants.Epsilon)
                    throw new ArgumentOutOfRangeException(nameof(LocalScale), "Scale can't be zero in any direction.");
                localScale = value;
                InvalidateCachedData();
            }
        }

        private Matrix4x4 localModel;
        public Matrix4x4 LocalModel
        {
            get
            {
                if (requiresCachedDataRecalculation)
                    RecalculateCachedData();
                return localModel;
            }
        }

        private Matrix4x4 localView;
        public Matrix4x4 LocalView
        {
            get
            {
                if (requiresCachedDataRecalculation)
                    RecalculateCachedData();
                return localView;
            }
        }

        private Vector3 position;
        public Vector3 Position
        {
            get
            {
                if (requiresCachedDataRecalculation)
                    RecalculateCachedData();
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

        private Quaternion rotation;
        public Quaternion Rotation
        {
            get
            {
                if (requiresCachedDataRecalculation)
                    RecalculateCachedData();
                return rotation;
            }
            set
            {
                if (Parent == null)
                {
                    LocalRotation = value;
                    return;
                }

                DecomposeModel(Parent.View * CreateModel(Position, value, Scale), out _, out Quaternion rot, out _);

                LocalRotation = rot;
            }
        }

        private Vector3 scale;
        private Vector3 Scale
        {
            get
            {
                if (requiresCachedDataRecalculation)
                    RecalculateCachedData();
                return scale;
            }
        }

        private Matrix4x4 model;
        public Matrix4x4 Model
        {
            get
            {
                if (requiresCachedDataRecalculation)
                    RecalculateCachedData();
                return model;
            }
        }

        private Matrix4x4 view;
        public Matrix4x4 View
        {
            get
            {
                if (requiresCachedDataRecalculation)
                    RecalculateCachedData();
                return view;
            }
        }

        public Vector3 LocalForward => LocalRotation * Vector3.Forward;
        public Vector3 LocalRight => LocalRotation * Vector3.Right;
        public Vector3 LocalUp => LocalRotation * Vector3.Up;
        public Vector3 Forward => Rotation * Vector3.Forward;
        public Vector3 Right => Rotation * Vector3.Right;
        public Vector3 Up => Rotation * Vector3.Up;


        public Transform()
        {
            localPosition = Vector3.Zero;
            localRotation = Quaternion.Identity;
            localScale = new Vector3(1.0, 1.0, 1.0);

            RecalculateCachedData();
        }

        private void RecalculateCachedData()
        {
            localModel = CreateModel(localPosition, localRotation, localScale);
            model = Parent == null ? localModel : Parent.Model * localModel;

            localView = CreateView(localPosition, localRotation, localScale);
            view = Parent == null ? localView : localView * Parent.View;

            DecomposeModel(model, out position, out rotation, out scale);

            requiresCachedDataRecalculation = false;
        }

        private void DecomposeModel(in Matrix4x4 model, out Vector3 position, out Quaternion rotation, out Vector3 scale)
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

        private Matrix4x4 CreateModel(in Vector3 position, in Quaternion rotation, in Vector3 scale)
        {
            // Model * vec => Move * Rotate * Scale * vec
            Matrix4x4 res = Matrix4x4.FromQuaternion(rotation);

            res.v00 *= scale.x;
            res.v01 *= scale.y;
            res.v02 *= scale.z;
            res.v10 *= scale.x;
            res.v11 *= scale.y;
            res.v12 *= scale.z;
            res.v20 *= scale.x;
            res.v21 *= scale.y;
            res.v22 *= scale.z;

            res.v03 = position.x;
            res.v13 = position.y;
            res.v23 = position.z;

            return res;
        }

        private Matrix4x4 CreateView(in Vector3 position, in Quaternion rotation, in Vector3 scale)
        {
            // View * vec => Scale^(-1) * Rotate^(-1) * Move^(-1) * vec
            Matrix4x4 res = Matrix4x4.FromQuaternion(rotation);
            res.transpose();

            double invScaleX = 1.0 / scale.x;
            double invScaleY = 1.0 / scale.y;
            double invScaleZ = 1.0 / scale.z;
            res.v00 *= invScaleX;
            res.v01 *= invScaleX;
            res.v02 *= invScaleX;
            res.v10 *= invScaleY;
            res.v11 *= invScaleY;
            res.v12 *= invScaleY;
            res.v20 *= invScaleZ;
            res.v21 *= invScaleZ;
            res.v22 *= invScaleZ;

            res.v03 = -position.x * res.v00 - position.y * res.v01 - position.z * res.v02;
            res.v13 = -position.x * res.v10 - position.y * res.v11 - position.z * res.v12;
            res.v23 = -position.x * res.v20 - position.y * res.v21 - position.z * res.v22;

            return res;
        }
    }
}