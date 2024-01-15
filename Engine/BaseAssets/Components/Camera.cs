using System;
using System.Reflection;

using Engine.Graphics;

using LinearAlgebra;

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Engine.BaseAssets.Components
{
    public class Camera : BehaviourComponent
    {

        #region ComponentLogic

        [SerializedField]
        private double fov = Math.PI / 2;
        [SerializedField]
        private double aspect;
        [SerializedField]
        private double near;
        [SerializedField]
        private double far;

        private static Camera current = null;
        public static Camera Current
        {
            get => current;
            set => current = value;
        }
        public bool IsCurrent => Current == this;

        public int Width { get; private set; }
        public int Height { get; private set; }

        public double FOV
        {
            get => fov;
            set => SetAndInvalidateMatrices(out fov, value);
        }
        public double Aspect
        {
            get => aspect;
            set => SetAndInvalidateMatrices(out aspect, value);
        }
        public double Near
        {
            get => near;
            set => SetAndInvalidateMatrices(out near, value);
        }
        public double Far
        {
            get => far;
            set => SetAndInvalidateMatrices(out far, value);
        }

        public Color BackgroundColor { get; set; }

        public Camera()
        {
            BackgroundColor = Color.FromRgba(0xFF010101);
            //BackgroundColor = Color.FromRgba(0xFFFFFFFF);
            Resize(1280, 720);
        }

        public void MakeCurrent()
        {
            Current = this;
        }

        public void Resize(int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));

            if (width == targetWidth && height == targetHeight)
                return;
            
            targetHeight = height;
            targetWidth = width;
            needsToBeResized = true;
        }

        internal override void OnDeserialized()
        {
            InvalidateMatrices();
        }

        public override void OnFieldChanged(FieldInfo fieldInfo)
        {
            InvalidateMatrices();
        }

        #endregion ComponentLogic

        #region Matrices

        private Matrix4x4 proj;
        public Matrix4x4 Proj
        {
            get
            {
                if (matricesRequireRecalculation)
                    RecalculateMatrices();
                return proj;
            }
        }

        private Matrix4x4 invProj;
        public Matrix4x4 InvProj
        {
            get
            {
                if (matricesRequireRecalculation)
                    RecalculateMatrices();
                return invProj;
            }
        }

        private bool matricesRequireRecalculation;

        private void SetAndInvalidateMatrices(out double field, double value)
        {
            field = value;
            InvalidateMatrices();
        }

        private void InvalidateMatrices()
        {
            matricesRequireRecalculation = true;
        }

        private void RecalculateMatrices()
        {
            double ctg = 1 / Math.Tan(FOV / 2);

            proj = new Matrix4x4(ctg / aspect, 0, 0, 0,
                                 0, 0, ctg, 0,
                                 0, far / (far - near), 0, -far * near / (far - near),
                                 0, 1, 0, 0);

            invProj = proj.inverse();

            matricesRequireRecalculation = false;
        }

        #endregion Matrices

        #region Render

        public event Action OnResized;

        internal GBuffer GBuffer { get; private set; }
        internal Texture DepthBuffer { get; private set; }
        internal Texture RadianceBuffer { get; private set; }
        internal Texture ColorBuffer { get; private set; }

        internal FrameBuffer BackBuffer { get; private set; }
        private FrameBuffer middleBuffer;
        private FrameBuffer frontBuffer;

        private int targetWidth;
        private int targetHeight;
        private bool needsToBeResized;

        internal void PreRenderUpdate()
        {
            if (needsToBeResized)
            {
                needsToBeResized = false;
                GenerateBuffers(targetWidth, targetHeight);
                OnResized?.Invoke();
            }
        }

        private void GenerateBuffers(int width, int height)
        {
            Width = width;
            Height = height;

            frontBuffer?.Dispose();
            middleBuffer?.Dispose();
            BackBuffer?.Dispose();

            GBuffer.Dispose();
            DepthBuffer?.Dispose();
            RadianceBuffer?.Dispose();
            ColorBuffer?.Dispose();

            frontBuffer = new FrameBuffer(width, height);
            middleBuffer = new FrameBuffer(width, height);
            BackBuffer = new FrameBuffer(width, height);

            GBuffer = new GBuffer(width, height);
            DepthBuffer = new Texture(width, height, null, Format.R32_Typeless, BindFlags.DepthStencil | BindFlags.ShaderResource);
            RadianceBuffer = new Texture(width, height, null, Format.R32G32B32A32_Float, BindFlags.ShaderResource | BindFlags.RenderTarget);
            ColorBuffer = new Texture(width, height, null, Format.R32G32B32A32_Float, BindFlags.ShaderResource | BindFlags.RenderTarget);
        }

        internal void SwapFrameBuffers()
        {
            lock (middleBuffer)
            {
                (BackBuffer, middleBuffer) = (middleBuffer, BackBuffer);
            }
        }

        public FrameBuffer GetNextFrontBuffer()
        {
            lock (middleBuffer)
            {
                (middleBuffer, frontBuffer) = (frontBuffer, middleBuffer);
            }
            return frontBuffer;
        }

        #endregion Render

        #region D9Render

        private D9CameraRenderer d9Renderer;
        public D9CameraRenderer D9Renderer => d9Renderer ??= new D9CameraRenderer(this);

        #endregion D9Render

    }
}