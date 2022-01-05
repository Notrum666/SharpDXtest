using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpDXtest.BaseAssets.Components
{
    public class Transform : Component
    {
        public Transform Parent { get; private set; }
        public Vector3 localPosition;
        public Vector3 Position 
        { 
            get 
            {
                return Parent == null ? localPosition : (Parent.model * new Vector4(localPosition, 1.0)).xyz;
            } 
            set
            {
                if (Parent == null)
                    localPosition = value;
                else
                    localPosition = (new Vector4(value, 1.0) * Parent.view).xyz;
            }
        }
        public Quaternion localRotation;
        public Quaternion Rotation
        {
            get
            {
                return Parent == null ? localRotation : Parent.Rotation.inverse() * localRotation;
            }
            set
            {
                if (Parent == null)
                    localRotation = value;
                else
                    localRotation = Parent.Rotation * value;
            }
        }
        public Matrix4x4 localModel
        {
            get
            {
                Matrix4x4 mat = Matrix4x4.FromQuaternion(localRotation);
                mat.v03 = localPosition.x;
                mat.v13 = localPosition.y;
                mat.v23 = localPosition.z;
                return mat;
            }
        }
        public Matrix4x4 model
        {
            get
            {
                return Parent == null ? localModel : Parent.model * localModel;
            }
        }
        public Matrix4x4 localView
        {
            get
            {
                Vector3 r = localRight;
                Vector3 u = localUp;
                Vector3 f = localForward;
                Vector3 p = -localPosition;

                Matrix4x4 view = new Matrix4x4(r.x, r.y, r.z, p * r,
                                               f.x, f.y, f.z, p * f,
                                               u.x, u.y, u.z, p * u,
                                               0, 0, 0, 1);

                return view;
            }
        }
        public Matrix4x4 view
        {
            get
            {
                return Parent == null ? localView : localView * Parent.view;
            }
        }
        public Vector3 localForward { get { return (localModel * new Vector4(Vector3.Forward)).xyz; } }
        public Vector3 localRight { get { return (localModel * new Vector4(Vector3.Right)).xyz; } }
        public Vector3 localUp { get { return (localModel * new Vector4(Vector3.Up)).xyz; } }
        public Vector3 forward { get { return (model * new Vector4(Vector3.Forward)).xyz; } }
        public Vector3 right { get { return (model * new Vector4(Vector3.Right)).xyz; } }
        public Vector3 up { get { return (model * new Vector4(Vector3.Up)).xyz; } }
        public Transform()
        {
            localPosition = Vector3.Zero;
            localRotation = Quaternion.Identity;
        }
        public void setParent(Transform transform)
        {
            Parent = transform;
        }
    }
}
