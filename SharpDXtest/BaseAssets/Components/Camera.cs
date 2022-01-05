using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpDXtest.BaseAssets.Components
{
    public class Camera : Component
    {
        public static Camera Current { get; private set; }

        public double FOV = Math.PI / 2;
        public double resolution;
        public double near;
        public double far;

        public Matrix4x4 proj
        {
            get
            {
                double ctg = 1 / Math.Tan(FOV / 2);

                Matrix4x4 proj = new Matrix4x4(ctg / resolution, 0, 0, 0,
                                               0, 0, ctg, 0,
                                               0, far / (far - near), 0, -far * near / (far - near),
                                               0, 1, 0, 0);
                return proj;
            }
        }
        public void MakeCurrent()
        {
            Camera.Current = this;
        }
    }
}
