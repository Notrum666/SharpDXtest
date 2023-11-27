using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;
using LinearAlgebra;

namespace Engine.BaseAssets.Components
{
    public class SpotLight : Light
    {
        private float radius = 1.0f;
        public float Radius
        {
            get => radius;
            set
            {
                if (value < 0.0f)
                    throw new ArgumentOutOfRangeException("Radius", "Radius can't be negative");
                radius = value;
            }
        }
        private float intensity = 0.4f;
        public float Intensity
        {
            get => intensity;
            set
            {
                if (value < 0.0f || value > 1.0f)
                    throw new ArgumentOutOfRangeException("Intensity", "Intensity can't be negative or more than 1");
                intensity = value;
            }
        }
        private float angularIntensity = 0.4f;
        public float AngularIntensity
        {
            get => angularIntensity;
            set
            {
                if (value < 0.0f || value > 1.0f)
                    throw new ArgumentOutOfRangeException("AngularIntensity", "Angular intensity can't be negative or more than 1");
                angularIntensity = value;
            }
        }
        private float angle = (float)Math.PI / 3.0f;
        public float Angle
        {
            get => angle;
            set
            {
                if (value < 0.0f || value > Math.PI)
                    throw new ArgumentOutOfRangeException("Angle", "Angle can't be negative or more than PI");
                angle = value;
            }
        }
        public static readonly float NEAR = 0.001f;
        private int shadowSize = 1024;
        public int ShadowSize
        {
            get => shadowSize;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("ShadowSize", "Shadow size must be a positive value.");
                shadowSize = value;
            }
        }
        public Matrix4x4f lightSpace
        {
            get
            {
                float ctg = 1f / (float)Math.Tan(angle / 2f);

                Matrix4x4f proj = new Matrix4x4f(ctg, 0, 0, 0,
                                                 0, 0, ctg, 0,
                                                 0, radius / (radius - NEAR), 0, -radius * NEAR / (radius - NEAR),
                                                 0, 1, 0, 0);

                return proj * (Matrix4x4f)GameObject.Transform.View;
            }
        }
        public Texture ShadowTexture { get; private set; }

        public SpotLight()
        {
            ShadowTexture = new Texture(shadowSize, shadowSize, null, SharpDX.DXGI.Format.R32_Typeless, BindFlags.ShaderResource | BindFlags.DepthStencil);
        }
    }
}