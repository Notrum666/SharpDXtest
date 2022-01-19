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
                return Parent == null ? localPosition : (Parent.Model * new Vector4(localPosition, 1.0)).xyz;
            } 
            set
            {
                if (Parent == null)
                    localPosition = value;
                else
                    localPosition = (Parent.View * new Vector4(value, 1.0)).xyz;
            }
        }
        public Quaternion localRotation;
        public Quaternion Rotation
        {
            get
            {
                return Parent == null ? localRotation : Parent.Rotation * localRotation;
            }
            set
            {
                if (Parent == null)
                    localRotation = value;
                else
                    localRotation = Parent.Rotation.inverse() * value;
            }
        }
        public Matrix4x4 LocalModel
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
        public Matrix4x4 Model
        {
            get
            {
                return Parent == null ? LocalModel : Parent.Model * LocalModel;
            }
        }
        public Matrix4x4 LocalView
        {
            get
            {
                Matrix4x4 view = Matrix4x4.FromQuaternion(localRotation).transposed();
                view.v03 = -localPosition.x * view.v00 - localPosition.y * view.v01 - localPosition.z * view.v02;
                view.v13 = -localPosition.x * view.v10 - localPosition.y * view.v11 - localPosition.z * view.v12;
                view.v23 = -localPosition.x * view.v20 - localPosition.y * view.v21 - localPosition.z * view.v22;
                return view;
            }
        }
        public Matrix4x4 View
        {
            get
            {
                return Parent == null ? LocalView : LocalView * Parent.View;
            }
        }
        public Vector3 LocalForward { get { return localRotation * Vector3.Forward; } }
        public Vector3 LocalRight { get { return localRotation * Vector3.Right; } }
        public Vector3 LocalUp { get { return localRotation * Vector3.Up; } }
        public Vector3 Forward { get { return Rotation * Vector3.Forward; } }
        public Vector3 Right { get { return Rotation * Vector3.Right; } }
        public Vector3 Up { get { return Rotation * Vector3.Up; } }
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
