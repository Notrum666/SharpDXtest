using System;
using LinearAlgebra;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Engine.BaseAssets.Components
{
    public class Camera : Component
    {
        private double fov = Math.PI / 2;
        public double FOV
        {
            get => fov;
            set
            {
                fov = value;
                InvalidateMatrixes();
            }
        }
        private double aspect;
        public double Aspect
        {
            get => aspect;
            set
            {
                aspect = value;
                InvalidateMatrixes();
            }
        }
        private double near;
        public double Near
        {
            get => near;
            set
            {
                near = value;
                InvalidateMatrixes();
            }
        }
        private double far;
        public double Far
        {
            get => far;
            set
            {
                far = value;
                InvalidateMatrixes();
            }
        }
        public static Camera Current
        {
            get => GraphicsCore.CurrentCamera;
            set => GraphicsCore.CurrentCamera = value;
        }
        public bool IsCurrent => Current == this;
        private Matrix4x4 proj;
        public Matrix4x4 Proj
        {
            get
            {
                if (matrixesRequireRecalculation)
                    RecalculateMatrixes();
                return proj;
            }
        }
        private Matrix4x4 invProj;
        public Matrix4x4 InvProj
        {
            get
            {
                if (matrixesRequireRecalculation)
                    RecalculateMatrixes();
                return invProj;
            }
        }
        private bool matrixesRequireRecalculation;

        public event Action<Camera> OnResized;

        public Color BackgroundColor { get; set; }

        internal GBuffer GBuffer { get; private set; }
        internal Texture DepthBuffer { get; private set; }
        internal Texture RadianceBuffer { get; private set; }
        internal Texture ColorBuffer { get; private set; }
        internal FrameBuffer Backbuffer { get; private set; }
        private FrameBuffer middlebuffer;
        private FrameBuffer frontbuffer;

        private bool needsToBeResized;
        private int targetWidth;
        private int targetHeight;

        public int Width { get; private set; }
        public int Height { get; private set; }

        public Camera()
        {
            BackgroundColor = Color.FromRgba(0xFF010101);
            //BackgroundColor = Color.FromRgba(0xFFFFFFFF);
        }

        public void InvalidateMatrixes()
        {
            matrixesRequireRecalculation = true;
        }

        public void RecalculateMatrixes()
        {
            double ctg = 1 / Math.Tan(FOV / 2);

            proj = new Matrix4x4(ctg / aspect, 0, 0, 0,
                                 0, 0, ctg, 0,
                                 0, far / (far - near), 0, -far * near / (far - near),
                                 0, 1, 0, 0);

            invProj = proj.inverse();

            matrixesRequireRecalculation = false;
        }

        public void MakeCurrent()
        {
            Current = this;
            Resize(1280, 720);
        }

        public void Resize(int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));

            targetHeight = height;
            targetWidth = width;
            needsToBeResized = true;
        }

        internal void PreRenderUpdate()
        {
            if (needsToBeResized)
            {
                needsToBeResized = false;
                GenerateBuffers(targetWidth, targetHeight);
                OnResized?.Invoke(this);
            }
        }

        private void GenerateBuffers(int width, int height)
        {
            Width = width;
            Height = height;
            frontbuffer?.Dispose();
            middlebuffer?.Dispose();
            Backbuffer?.Dispose();
            GBuffer.worldPos?.Dispose();
            GBuffer.albedo?.Dispose();
            GBuffer.normal?.Dispose();
            GBuffer.metallic?.Dispose();
            GBuffer.roughness?.Dispose();
            GBuffer.ambientOcclusion?.Dispose();
            DepthBuffer?.Dispose();
            RadianceBuffer?.Dispose();
            ColorBuffer?.Dispose();
            frontbuffer = new FrameBuffer(width, height);
            middlebuffer = new FrameBuffer(width, height);
            Backbuffer = new FrameBuffer(width, height);
            GBuffer = new GBuffer(width, height);
            DepthBuffer = new Texture(width, height, null, Format.R32_Typeless, BindFlags.DepthStencil | BindFlags.ShaderResource);
            RadianceBuffer = new Texture(width, height, null, Format.R32G32B32A32_Float, BindFlags.ShaderResource | BindFlags.RenderTarget);
            ColorBuffer = new Texture(width, height, null, Format.R32G32B32A32_Float, BindFlags.ShaderResource | BindFlags.RenderTarget);
        }

        public void SwapFrameBuffers()
        {
            lock (middlebuffer)
            {
                FrameBuffer tmp = Backbuffer;
                Backbuffer = middlebuffer;
                middlebuffer = tmp;
            }
        }

        public FrameBuffer GetNextFrontBuffer()
        {
            lock (middlebuffer)
            {
                FrameBuffer tmp = middlebuffer;
                middlebuffer = frontbuffer;
                frontbuffer = tmp;
            }
            return frontbuffer;
        }
    }
}