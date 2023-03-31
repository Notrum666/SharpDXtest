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
                InvalidateMatrixes();
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
                InvalidateMatrixes();
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
                InvalidateMatrixes();
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
                InvalidateMatrixes();
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
                if (value != null && value.GameObject == null)
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
                if (matrixesRequireRecalculation)
                    RecalculateMatrixes();
                return proj;
            }
        }
        private Matrix4x4 invProj;
        public Matrix4x4 InvProj
        {
            get
            {
                if (matrixesRequireRecalculation)
                    RecalculateMatrixes();
                return invProj;
            }
        }
        private bool matrixesRequireRecalculation;
        public void InvalidateMatrixes()
        {
            matrixesRequireRecalculation = true;
        }
        public void RecalculateMatrixes()
        {
            double ctg = 1 / Math.Tan(FOV / 2);

            proj = new Matrix4x4(ctg / aspect, 0, 0, 0,
                                 0, 0, ctg, 0,
                                 0, far / (far - near), 0, -far * near / (far - near),
                                 0, 1, 0, 0);

            invProj = proj.inverse();

            matrixesRequireRecalculation = false;
        }
    }
}
