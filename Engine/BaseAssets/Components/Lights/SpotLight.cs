using System;

using LinearAlgebra;

using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Engine.BaseAssets.Components
{
    public class SpotLight : Light
    {
        private const float Near = 0.001f;

        [SerializedField]
        private float radius = 1.0f;
        [SerializedField]
        private float intensity = 0.4f;
        [SerializedField]
        private float angularIntensity = 0.4f;
        [SerializedField]
        private float angle = (float)Math.PI / 3.0f;
        [SerializedField]
        private int shadowSize = 1024;

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

        public Matrix4x4f LightSpace
        {
            get
            {
                float ctg = 1f / (float)Math.Tan(angle / 2f);

                Matrix4x4f proj = new Matrix4x4f(ctg, 0, 0, 0,
                                                 0, 0, ctg, 0,
                                                 0, radius / (radius - Near), 0, -radius * Near / (radius - Near),
                                                 0, 1, 0, 0);

                return proj * (Matrix4x4f)GameObject.Transform.View;
            }
        }

        public Texture ShadowTexture { get; private set; }

        public SpotLight()
        {
            ShadowTexture = new Texture(shadowSize, shadowSize, null, Format.R32_Typeless, BindFlags.ShaderResource | BindFlags.DepthStencil);
        }
    }
}