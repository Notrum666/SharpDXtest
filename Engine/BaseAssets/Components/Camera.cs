using System;
using System.Collections.Generic;
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
        internal static readonly List<Camera> Cameras = new List<Camera>();

        #region ComponentLogic

        [SerializedField]
        private bool makeCurrentOnStart;
        [SerializedField]
        private double fov = Math.PI / 2;
        [SerializedField]
        private double aspect = 1;
        [SerializedField]
        private double near = 0.001;
        [SerializedField]
        private double far = 500;

        private static Camera current = null;
        public static Camera Current
        {
            get => current;
            private set => current = value;
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

        private protected override void InitializeInner()
        {
            base.InitializeInner();
            Cameras.Add(this);
        }

        public override void Start()
        {
            if (makeCurrentOnStart)
                MakeCurrent();
        }

        protected override void OnDestroy()
        {
            if (Current == this)
                Current = null;

            Cameras.Remove(this);
        }

        public void MakeCurrent()
        {
            Current = this;
        }

        public void Resize(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                Logger.Log(LogType.Error, $"Tried to resize Camera to invalid size, size must be positive, but ({width}, {height}) were given.");
                return;
            }

            if (width == targetWidth && height == targetHeight)
                return;

            targetHeight = height;
            targetWidth = width;
            NeedsToBeResized = true;
        }

        internal override void OnDeserialized()
        {
            InvalidateMatrices();
        }

        public override void OnFieldChanged(FieldInfo fieldInfo)
        {
            InvalidateMatrices();
        }

        /// <summary>
        /// Transforms screen pixel position to world direction vector originating from camera
        /// </summary>
        /// <param name="mousePos">Pixel position, where X is left to right and Y is top to bottom</param>
        /// <returns>Direction vector originating from camera position</returns>
        public LinearAlgebra.Vector3 ScreenToWorld(LinearAlgebra.Vector2 pixelPos)
        {
            pixelPos.x = pixelPos.x / Width * 2 - 1.0;
            pixelPos.y = 1.0 - pixelPos.y / Height * 2;
            return ((GameObject.Transform.Model * InvProj).TransformPoint(new LinearAlgebra.Vector3(pixelPos, 1.0)) - GameObject.Transform.Position).normalized();
        }

        /// <summary>
        /// Transforms world point to screen pixel position
        /// </summary>
        /// <param name="point">Point in world space</param>
        /// <returns>Pixel position, where X is left to right and Y is top to bottom</returns>
        public LinearAlgebra.Vector2 WorldToScreen(LinearAlgebra.Vector3 point)
        {
            point = (GameObject.Transform.View * Proj).TransformPoint(point - GameObject.Transform.Position);
            return new LinearAlgebra.Vector2((point.x + 1.0) / 2.0 * Width, -(point.y + 1.0) / 2.0 * Height);
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
        internal bool NeedsToBeResized { get; private set; }

        /// <summary>
        /// Resizes Camera and recreates buffers if needed. <br/>
        /// Executed every RenderUpdate
        /// </summary>
        internal void PreRenderUpdate()
        {
            if (NeedsToBeResized)
            {
                GenerateBuffers(targetWidth, targetHeight);
                NeedsToBeResized = false;
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
                renderReady = true;
            }
        }

        #endregion Render

        #region D9Render

        public D9CameraRenderer D9Renderer => d9Renderer ??= new D9CameraRenderer(this);
        internal bool ShouldRender => d9Renderer?.ViewersCount > 0 && GameObject != null && Enabled;

        private D9CameraRenderer d9Renderer;
        private bool renderReady;

        public void DrawFrontBuffer()
        {
            if (middleBuffer == null || !renderReady)
                return;

            lock (middleBuffer)
            {
                (middleBuffer, frontBuffer) = (frontBuffer, middleBuffer);
                renderReady = false;
            }

            d9Renderer?.Draw(frontBuffer);
        }

        #endregion D9Render

    }
}