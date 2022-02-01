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
        public static readonly float NEAR = 0.001f;
        public static readonly int SHADOW_SIZE = 2048;
        //public int FBO { get; private set; } = 0;
        //public int shadowTex { get; private set; } = 0;
        public Matrix4x4 lightSpace
        {
            get
            {
                float ctg = 1f / (float)Math.Tan(angle / 2f);

                Matrix4x4 proj = new Matrix4x4(ctg, 0, 0, 0,
                                               0, 0, ctg, 0,
                                               0, radius / (radius - NEAR), 0, -radius * NEAR / (radius - NEAR),
                                               0, 1, 0, 0);
                return proj * gameObject.transform.View;
            }
        }
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
