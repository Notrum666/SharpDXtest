using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX.Direct3D11;
using SharpDX.DXGI;

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
        public static readonly int SHADOW_SIZE = 4096;
        public Matrix4x4 lightSpace
        {
            get
            {
                Matrix4x4 view = Matrix4x4.FromQuaternion(gameObject.transform.Rotation).transposed();
                Vector3 pos = GraphicsCore.CurrentCamera.gameObject.transform.Position;
                view.v03 = -pos.x * view.v00 - pos.y * view.v01 - pos.z * view.v02;
                view.v13 = -pos.x * view.v10 - pos.y * view.v11 - pos.z * view.v12;
                view.v23 = -pos.x * view.v20 - pos.y * view.v21 - pos.z * view.v22;

                Matrix4x4 ortho = new Matrix4x4(1, 0, 0, 0,
                                                0, 0, 1, 0,
                                                0, 0.5, 0, 0.5 * radius,
                                                0, 0, 0, radius);

                return ortho * view;
            }
        }
        public Texture ShadowTexture { get; private set; }
        public DirectionalLight()
        {
            //shadowTex = new Texture2D(GraphicsCore.CurrentDevice, new Texture2DDescription()
            //{
            //    Width = SHADOW_SIZE,
            //    Height = SHADOW_SIZE,
            //    ArraySize = 1,
            //    BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
            //    Usage = ResourceUsage.Immutable,
            //    CpuAccessFlags = CpuAccessFlags.None,
            //    Format = Format.D32_Float,
            //    MipLevels = 1,
            //    OptionFlags = ResourceOptionFlags.None,
            //    SampleDescription = new SampleDescription(1, 0)
            //});
            //
            //textureView = new ShaderResourceView(GraphicsCore.CurrentDevice, shadowTex);
            //depthView = new DepthStencilView(GraphicsCore.CurrentDevice, shadowTex);

            ShadowTexture = new Texture(SHADOW_SIZE, SHADOW_SIZE, usage: BindFlags.ShaderResource | BindFlags.DepthStencil);
            //ShadowTexture = new Texture(SHADOW_SIZE, SHADOW_SIZE, Vector4f.Zero, usage: BindFlags.ShaderResource | BindFlags.RenderTarget);

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
