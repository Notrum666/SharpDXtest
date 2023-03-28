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
        private static readonly float[] cascadeFrustumDistances = { 0.0f, 0.1f, 0.3f, 1.0f };
        public static float[] CascadeFrustumDistances { get => cascadeFrustumDistances; }
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
        private Matrix4x4f getLightSpaceForFrustumSlice(Matrix4x4f frustumToView, float fromZ, float toZ, float lightSpaceDepthScale)
        {
            Vector3f[] corners = new Vector3f[8];
            corners[0] = frustumToView.TransformPoint(new Vector3f(-1.0f, -1.0f, fromZ));
            corners[1] = frustumToView.TransformPoint(new Vector3f( 1.0f, -1.0f, fromZ));
            corners[2] = frustumToView.TransformPoint(new Vector3f(-1.0f,  1.0f, fromZ));
            corners[3] = frustumToView.TransformPoint(new Vector3f( 1.0f,  1.0f, fromZ));
            corners[4] = frustumToView.TransformPoint(new Vector3f(-1.0f, -1.0f, toZ));
            corners[5] = frustumToView.TransformPoint(new Vector3f( 1.0f, -1.0f, toZ));
            corners[6] = frustumToView.TransformPoint(new Vector3f(-1.0f,  1.0f, toZ));
            corners[7] = frustumToView.TransformPoint(new Vector3f( 1.0f,  1.0f, toZ));

            float minX = corners[0].x;
            float maxX = corners[0].x;
            float minY = corners[0].y;
            float maxY = corners[0].y;
            float minZ = corners[0].z;
            float maxZ = corners[0].z;

            for (int i = 1; i < corners.Length; i++)
            {
                if (corners[i].x < minX)
                    minX = corners[i].x;
                if (corners[i].x > maxX)
                    maxX = corners[i].x;
                if (corners[i].y < minY)
                    minY = corners[i].y;
                if (corners[i].y > maxY)
                    maxY = corners[i].y;
                if (corners[i].z < minZ)
                    minZ = corners[i].z;
                if (corners[i].z > maxZ)
                    maxZ = corners[i].z;
            }

            minY = maxY - (maxY - minY) * lightSpaceDepthScale;

            float invDx = 2.0f / (maxX - minX);
            float invDy = 1.0f / (maxY - minY);
            float invDz = 2.0f / (maxZ - minZ);

            return new Matrix4x4f(invDx, 0.0f, 0.0f, -minX * invDx - 1.0f,
                                  0.0f, 0.0f, invDz, -minZ * invDz - 1.0f,
                                  0.0f, invDy, 0.0f, -minY * invDy,
                                  0.0f, 0.0f, 0.0f, 1.0f);
        }
        public Matrix4x4f[] GetLightSpaces(Camera camera)
        {
            Matrix4x4f view = (Matrix4x4f)gameObject.transform.View;
            Matrix4x4f frustumToView = view * (Matrix4x4f)(camera.gameObject.transform.Model * camera.InvProj);

            double f = camera.Far;
            double n = camera.Near;
            double delinearizeDepth(double z)
            {
                z = n + (f - n) * z;
                return f * (z - n) / ((f - n) * z);
            }

            Matrix4x4f[] result = new Matrix4x4f[cascadeFrustumDistances.Length - 1];
            for (int i = 0; i < cascadeFrustumDistances.Length - 1; i++)
                result[i] = getLightSpaceForFrustumSlice(frustumToView, (float)delinearizeDepth(cascadeFrustumDistances[i]), (float)delinearizeDepth(cascadeFrustumDistances[i + 1]), 100.0f) * view;

            return result;
        }
        public Texture ShadowTexture { get; private set; }
        public DirectionalLight()
        {
            ShadowTexture = new Texture(shadowSize, shadowSize, 0.0f.GetBytes(), SharpDX.DXGI.Format.R32_Typeless, BindFlags.ShaderResource | BindFlags.DepthStencil, cascadeFrustumDistances.Length - 1);
        }
    }
}
