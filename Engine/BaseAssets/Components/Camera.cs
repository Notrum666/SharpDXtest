using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LinearAlgebra;

namespace Engine.BaseAssets.Components
{
    public class Camera : Component
    {
        private double fov = Math.PI / 2;
        public double FOV
        {
            get
            {
                return fov;
            }
            set
            {
                fov = value;
                recalculateMatrixes();
            }
        }
        private double aspect;
        public double Aspect
        {
            get
            {
                return aspect;
            }
            set
            {
                aspect = value;
                recalculateMatrixes();
            }
        }
        private double near;
        public double Near
        {
            get
            {
                return near;
            }
            set
            {
                near = value;
                recalculateMatrixes();
            }
        }
        private double far;
        public double Far
        {
            get
            {
                return far;
            }
            set
            {
                far = value;
                recalculateMatrixes();
            }
        }
        private static Camera current = null;
        public static Camera Current
        {
            get
            {
                return current;
            }
            set
            {
                if (value != null && value.gameObject == null)
                    throw new Exception("Camera component must be attached to a gameobject.");

                current = value;
            }
        }
        public bool IsCurrent
        {
            get
            {
                return Current == this;
            }
            set
            {
                if (value)
                    Current = this;
                else
                {
                    if (Current == this)
                        Current = null;
                }
            }
        }
        private Matrix4x4 proj;
        public Matrix4x4 Proj
        {
            get
            {
                return proj;
            }
        }
        private void recalculateMatrixes()
        {
            double ctg = 1 / Math.Tan(FOV / 2);

            proj = new Matrix4x4(ctg / aspect, 0, 0, 0,
                                 0, 0, ctg, 0,
                                 0, far / (far - near), 0, -far * near / (far - near),
                                 0, 1, 0, 0);
        }
    }
}
