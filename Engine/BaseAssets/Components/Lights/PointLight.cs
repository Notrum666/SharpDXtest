using System;
using System.Reflection;

using LinearAlgebra;

namespace Engine.BaseAssets.Components
{
    public class PointLight : Light
    {
        [SerializedField]
        private float radius = 1.0f;
        [SerializedField]
        private float intensity = 0.4f;

        public Ranged<float> Radius => new Ranged<float>(ref radius, 0.0f);
        public Ranged<float> Intensity => new Ranged<float>(ref intensity, 0.0f, 1.0f);

        //public static readonly float NEAR = 0.001f;
        //public static readonly int SHADOW_SIZE = 1024;
        //public int FBO { get; private set; } = 0;
        //public int shadowCube { get; private set; } = 0;
        //public Matrix4[] lightSpaces
        //{
        //    get
        //    {
        //        float ctg = 1f / (float)Math.Tan(Math.PI / 4f);
        //        Vector3 r = gameObject.transform.right;
        //        Vector3 u = gameObject.transform.up;
        //        Vector3 f = gameObject.transform.forward;
        //        Vector3 p = -gameObject.transform.position;
        //        Matrix4 proj = new Matrix4(ctg, 0f, 0f, 0f,
        //                                   0f, ctg, 0f, 0f,
        //                                   0f, 0f, (radius + NEAR) / (radius - NEAR), -2f * radius * NEAR / (radius - NEAR),
        //                                   0f, 0f, 1f, 0f);
        //        Matrix4[] matrixes = new Matrix4[6];
        //        matrixes[0] = proj * new Matrix4(-f.X, -f.Y, -f.Z, Vector3.Dot(-f, p), // +x
        //                                        -u.X, -u.Y, -u.Z, Vector3.Dot(-u, p),
        //                                        r.X, r.Y, r.Z, Vector3.Dot(r, p),
        //                                        0f, 0f, 0f, 1f);
        //        matrixes[1] = proj * new Matrix4(f.X, f.Y, f.Z, Vector3.Dot(f, p), // -x
        //                                        -u.X, -u.Y, -u.Z, Vector3.Dot(-u, p),
        //                                        -r.X, -r.Y, -r.Z, Vector3.Dot(-r, p),
        //                                        0f, 0f, 0f, 1f);
        //        matrixes[2] = proj * new Matrix4(r.X, r.Y, r.Z, Vector3.Dot(r, p), // +y
        //                                         f.X, f.Y, f.Z, Vector3.Dot(f, p),
        //                                         u.X, u.Y, u.Z, Vector3.Dot(u, p),
        //                                         0f, 0f, 0f, 1f);
        //        matrixes[3] = proj * new Matrix4(r.X, r.Y, r.Z, Vector3.Dot(r, p), // -y
        //                                         -f.X, -f.Y, -f.Z, Vector3.Dot(-f, p),
        //                                         -u.X, -u.Y, -u.Z, Vector3.Dot(-u, p),
        //                                         0f, 0f, 0f, 1f);
        //        matrixes[4] = proj * new Matrix4(r.X, r.Y, r.Z, Vector3.Dot(r, p), // +z
        //                                        -u.X, -u.Y, -u.Z, Vector3.Dot(-u, p),
        //                                        f.X, f.Y, f.Z, Vector3.Dot(f, p),
        //                                        0f, 0f, 0f, 1f);
        //        matrixes[5] = proj * new Matrix4(-r.X, -r.Y, -r.Z, Vector3.Dot(-r, p), // -z
        //                                        -u.X, -u.Y, -u.Z, Vector3.Dot(-u, p),
        //                                        -f.X, -f.Y, -f.Z, Vector3.Dot(-f, p),
        //                                        0f, 0f, 0f, 1f);
        //        return matrixes;
        //    }
        //}
        public PointLight()
        {
            //FBO = GL.GenFramebuffer();
            //GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);
            //
            //shadowCube = GL.GenTexture();
            //GL.BindTexture(TextureTarget.TextureCubeMap, shadowCube);
            //
            //GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            //GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            //GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            //GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            //GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
            //GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.None);
            //
            //for (int i = 0; i < 6; i++)
            //    GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, PixelInternalFormat.DepthComponent, SHADOW_SIZE, SHADOW_SIZE, 0, PixelFormat.DepthComponent, PixelType.Float, 0);
            //
            //GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, shadowCube, 0);
            //
            //GL.DrawBuffer(DrawBufferMode.None);
            //GL.ReadBuffer(ReadBufferMode.None);
            //GL.BindTexture(TextureTarget.TextureCubeMap, 0);
            //GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public override void DoShadowPass()
        {
            // TODO: IMPLEMENT POINT LIGHT SHADOWS
        }

        public override void DoLightPass()
        {
            if (!ShaderPipeline.TryGetPipeline("deferred_light_point", out ShaderPipeline pipeline))
                return;

            pipeline.Use();

            Camera camera = Camera.Current;
            pipeline.UpdateUniform("camPos", (Vector3f)camera.GameObject.Transform.Position);

            pipeline.UpdateUniform("pointLight.position", (Vector3f)GameObject.Transform.Position);
            pipeline.UpdateUniform("pointLight.radius", Radius);
            pipeline.UpdateUniform("pointLight.brightness", Brightness);
            pipeline.UpdateUniform("pointLight.intensity", Intensity);
            pipeline.UpdateUniform("pointLight.color", Color);

            pipeline.UploadUpdatedUniforms();
        }
    }
}