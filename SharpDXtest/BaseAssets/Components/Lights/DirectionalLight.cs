using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpDXtest.BaseAssets.Components
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
        //public static readonly int SHADOW_SIZE = 4096;
        //public int FBO { get; private set; } = 0;
        //public int shadowTex { get; private set; } = 0;
        //public Matrix4x4 lightSpace
        //{
        //    get
        //    {
        //        Vector3 r = gameObject.transform.Right / radius;
        //        Vector3 u = gameObject.transform.up / radius;
        //        Vector3 f = gameObject.transform.forward / radius;
        //        Vector3 p = -Camera.Current.gameObject.transform.position;
        //        Matrix4 mat = new Matrix4(r.X, r.Y, r.Z, Vector3.Dot(p, r),
        //                                  u.X, u.Y, u.Z, Vector3.Dot(p, u),
        //                                  f.X, f.Y, f.Z, Vector3.Dot(p, f),
        //                                  0f, 0f, 0f, 1f);
        //        return mat;
        //    }
        //}
        public DirectionalLight()
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
