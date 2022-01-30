using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpDXtest.BaseAssets.Components
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
            get
            {
                return angularIntensity;
            }
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
        //public static readonly float NEAR = 0.001f;
        //public static readonly int SHADOW_SIZE = 2048;
        //public int FBO { get; private set; } = 0;
        //public int shadowTex { get; private set; } = 0;
        //public Matrix4 lightSpace
        //{
        //    get
        //    {
        //        float ctg = 1f / (float)Math.Tan(angle / 2f);
        //        Vector3 r = gameObject.transform.right * ctg;
        //        Vector3 u = gameObject.transform.up * ctg;
        //        Vector3 f = gameObject.transform.forward;
        //        Vector3 p = -gameObject.transform.position;
        //        float val = (radius + NEAR) / (radius - NEAR);
        //        Matrix4 mat = new Matrix4(r.X, r.Y, r.Z, Vector3.Dot(p, r),
        //                                  u.X, u.Y, u.Z, Vector3.Dot(p, u),
        //                                  f.X * val, f.Y * val, f.Z * val, Vector3.Dot(p, f) * val - 2f * radius * NEAR / (radius - NEAR),
        //                                  f.X, f.Y, f.Z, Vector3.Dot(p, f));
        //        return mat;
        //    }
        //}
        public SpotLight()
        {
            //FBO = GL.GenFramebuffer();
            //GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);
            //
            //shadowTex = GL.GenTexture();
            //GL.BindTexture(TextureTarget.Texture2D, shadowTex);
            //GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, SHADOW_SIZE, SHADOW_SIZE, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            //
            //GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, shadowTex, 0);
            //
            //GL.DrawBuffer(DrawBufferMode.None);
            //GL.ReadBuffer(ReadBufferMode.None);
            //GL.BindTexture(TextureTarget.Texture2D, 0);
            //GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
    }
}
