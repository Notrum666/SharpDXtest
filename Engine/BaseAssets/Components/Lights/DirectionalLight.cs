using System;

using LinearAlgebra;

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Engine.BaseAssets.Components
{
    public class DirectionalLight : Light
    {
        [SerializedField]
        private int shadowSize = 2048;

        public Ranged<int> ShadowSize => new Ranged<int>(ref shadowSize, 1);
        public static float[] CascadeFrustumDistances => cascadeFrustumDistances;

        private static readonly float[] cascadeFrustumDistances = { 0.0f, 0.1f, 0.3f, 1.0f };

        private Matrix4x4f getLightSpaceForFrustumSlice(Matrix4x4f frustumToView, float fromZ, float toZ, float lightSpaceDepthScale)
        {
            Vector3f[] corners = new Vector3f[8];
            corners[0] = frustumToView.TransformPoint(new Vector3f(-1.0f, -1.0f, fromZ));
            corners[1] = frustumToView.TransformPoint(new Vector3f(1.0f, -1.0f, fromZ));
            corners[2] = frustumToView.TransformPoint(new Vector3f(-1.0f, 1.0f, fromZ));
            corners[3] = frustumToView.TransformPoint(new Vector3f(1.0f, 1.0f, fromZ));
            corners[4] = frustumToView.TransformPoint(new Vector3f(-1.0f, -1.0f, toZ));
            corners[5] = frustumToView.TransformPoint(new Vector3f(1.0f, -1.0f, toZ));
            corners[6] = frustumToView.TransformPoint(new Vector3f(-1.0f, 1.0f, toZ));
            corners[7] = frustumToView.TransformPoint(new Vector3f(1.0f, 1.0f, toZ));

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
            Matrix4x4f view = (Matrix4x4f)GameObject.Transform.View;
            Matrix4x4f frustumToView = view * (Matrix4x4f)(camera.GameObject.Transform.Model * camera.InvProj);

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
            ShadowTexture = new Texture(shadowSize, shadowSize, null, Format.R32_Typeless, BindFlags.ShaderResource | BindFlags.DepthStencil, cascadeFrustumDistances.Length - 1);
        }

        public override void RenderShadows()
        {
            if (!ShaderPipeline.TryGetPipeline("depth_only", out ShaderPipeline pipeline))
                return;

            pipeline.Use();

            DeviceContext context = GraphicsCore.CurrentDevice.ImmediateContext;
            context.Rasterizer.SetViewport(new Viewport(0, 0, ShadowSize, ShadowSize, 0.0f, 1.0f));

            Matrix4x4f[] lightSpaces = GetLightSpaces(Camera.Current);
            for (int i = 0; i < lightSpaces.Length; i++)
            {
                DepthStencilView curDSV = ShadowTexture.GetView<DepthStencilView>(i);
                context.OutputMerger.SetTargets(curDSV, renderTargetView: null);
                context.ClearDepthStencilView(curDSV, DepthStencilClearFlags.Depth, 1.0f, 0);

                pipeline.UpdateUniform("view", lightSpaces[i]);

                RenderObjects(pipeline);
            }
        }

        public override bool PrepareLightPass(Camera camera)
        {
            if (!ShaderPipeline.TryGetPipeline("deferred_light_directional", out ShaderPipeline pipeline))
                return false;

            pipeline.Use();

            pipeline.UpdateUniform("camPos", (Vector3f)camera.GameObject.Transform.Position);

            pipeline.UpdateUniform("cam_NEAR", (float)camera.Near);
            pipeline.UpdateUniform("cam_FAR", (float)camera.Far);

            pipeline.UpdateUniform("directionalLight.direction", (Vector3f)GameObject.Transform.Forward);
            pipeline.UpdateUniform("directionalLight.brightness", Brightness);
            pipeline.UpdateUniform("directionalLight.color", Color);

            Matrix4x4f[] lightSpaces = GetLightSpaces(camera);
            for (int i = 0; i < lightSpaces.Length; i++)
                pipeline.UpdateUniform("directionalLight.lightSpaces[" + i.ToString() + "]", lightSpaces[i]);
            float[] cascadeDepths = DirectionalLight.CascadeFrustumDistances;
            for (int i = 0; i < cascadeDepths.Length; i++)
                pipeline.UpdateUniform("directionalLight.cascadesDepths[" + i.ToString() + "]", cascadeDepths[i]);
            pipeline.UpdateUniform("directionalLight.cascadesCount", lightSpaces.Length);

            pipeline.UpdateUniform("directionalLight.shadowMapSize", new Vector2f(ShadowSize, ShadowSize));

            pipeline.UploadUpdatedUniforms();

            ShadowTexture.Use("directionalLight.shadowMaps");
            GraphicsCore.ShadowsSampler.use("shadowSampler");
            camera.DepthBuffer.Use("depthTex");

            return true;
        }
    }
}