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
        public Vector3 position;
        public Quaternion rotation;
        public Matrix4x4 model
        {
            get
            {
                Matrix4x4 mat = Matrix4x4.FromQuaternion(rotation);
                mat.v03 = position.x;
                mat.v13 = position.y;
                mat.v23 = position.z;
                return Parent == null ? mat : Parent.model * mat;
            }
        }
        public Matrix4x4 view
        {
            get
            {
                Vector3 r = right;
                Vector3 u = up;
                Vector3 f = forward;
                Vector3 p = -position;

                Matrix4x4 view = new Matrix4x4(r.x, r.y, r.z, p * r,
                                               f.x, f.y, f.z, p * f,
                                               u.x, u.y, u.z, p * u,
                                               0, 0, 0, 1);

                return Parent == null ? view : view * Parent.view;
            }
        }
        public Vector3 forward { get { return (model * new Vector4(Vector3.Forward)).xyz; } }
        public Vector3 right { get { return (model * new Vector4(Vector3.Right)).xyz; } }
        public Vector3 up { get { return (model * new Vector4(Vector3.Up)).xyz; } }
        public Transform()
        {
            position = Vector3.Zero;
            rotation = Quaternion.Identity;
        }
        public void setParent(Transform transform)
        {
            Parent = transform;
        }
    }
}
