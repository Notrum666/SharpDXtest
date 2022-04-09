using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX.Direct3D11;

using LinearAlgebra;

namespace Engine.BaseAssets.Components
{
    public class DirectionalLight : Light
    {
        private float radius = 1.0f;
        public float Radius
        {
            get => radius;
            set
            {
                if (value <= 0.0f)
                    throw new ArgumentOutOfRangeException("Radius", "Radius can't be less or equal to zero");
                radius = value;
            }
        }
        private int shadowSize = 2048;
        public int ShadowSize
        {
            get
            {
                return shadowSize;
            }
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
                Matrix4x4f view = Matrix4x4f.FromQuaternion(gameObject.transform.Rotation).transposed();
                Vector3f pos = (Vector3f)GraphicsCore.CurrentCamera.gameObject.transform.Position;
                view.v03 = -pos.x * view.v00 - pos.y * view.v01 - pos.z * view.v02;
                view.v13 = -pos.x * view.v10 - pos.y * view.v11 - pos.z * view.v12;
                view.v23 = -pos.x * view.v20 - pos.y * view.v21 - pos.z * view.v22;

                Matrix4x4f ortho = new Matrix4x4f(1, 0, 0, 0,
                                                  0, 0, 1, 0,
                                                  0, 0.5f, 0, 0.5f * radius,
                                                  0, 0, 0, radius);

                return ortho * view;
            }
        }
        public Texture ShadowTexture { get; private set; }
        public DirectionalLight()
        {
            ShadowTexture = new Texture(shadowSize, shadowSize, 0.0f, BindFlags.ShaderResource | BindFlags.DepthStencil);
        }
    }
}
