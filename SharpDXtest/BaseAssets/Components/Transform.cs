using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpDXtest.BaseAssets.Components
{
    public class Transform : Component
    {
        public Transform Parent;
        public Vector3 position;
        public Quaternion rotation;
        public Matrix4x4 model
        {
            get
            {
                throw new NotImplementedException();
                //Matrix4x4 mat = Matrix4x4.CreateFromQuaternion(rotation);
                //mat.v03 = position.x;
                //mat.v13 = position.y;
                //mat.v23 = position.z;
                //return Parent == null ? mat : Parent.model * mat;
            }
        }
        //public Vector3 forward { get { return rotation * Vector3.forward; } }
        //public Vector3 right { get { return rotation * Vector3.right; } }
        //public Vector3 up { get { return rotation * Vector3.up; } }
        public Transform()
        {
            position = Vector3.Zero;
            rotation = Quaternion.Identity;
        }
    }
}
