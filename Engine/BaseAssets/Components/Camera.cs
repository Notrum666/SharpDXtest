using System;
using System.Reflection;
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

        public LinearAlgebra.Vector3 ScreenToWorldCoords(int x, int y)
        {
            Matrix4x4 viewMatrix = GameObject.Transform.View;
            Matrix4x4 projectionMatrix = Proj;

            LinearAlgebra.Vector2 mouse;
            mouse.x = x;
            mouse.y = y;

            LinearAlgebra.Vector3 vector = 
                UnProject(
                    projectionMatrix,
                    viewMatrix, 
                    new LinearAlgebra.Vector2(Width, Height), 
                    mouse
            );
            LinearAlgebra.Vector3 coords = new LinearAlgebra.Vector3(vector.x, vector.y, vector.z);
            return coords;
        }

        LinearAlgebra.Vector3 UnProject(Matrix4x4 projection, Matrix4x4 view, LinearAlgebra.Vector2 viewport, LinearAlgebra.Vector2 mouse)
        {
            LinearAlgebra.Vector3 vec;

            vec.x = (mouse.x / viewport.x) * 2 - 1;
            vec.y = 1 - (mouse.y / viewport.y) * 2;
            vec.z = 0;

            Matrix4x4 viewInv = view;
            Matrix4x4 projInv = projection;

            viewInv.invert();
            projInv.invert();

            vec = projInv.TransformPoint(vec);
            vec = viewInv.TransformPoint(vec);

            return vec;
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

        public event Action<Camera> OnResized;

        internal GBuffer GBuffer { get; private set; }
        internal Texture DepthBuffer { get; private set; }
        internal Texture RadianceBuffer { get; private set; }
        internal Texture ColorBuffer { get; private set; }

        internal FrameBuffer Backbuffer { get; private set; }
        private FrameBuffer middlebuffer;
        private FrameBuffer frontbuffer;

        private int targetWidth;
        private int targetHeight;
        private bool needsToBeResized;

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

            GBuffer.Dispose();
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

        internal void SwapFrameBuffers()
        {
            lock (middlebuffer)
            {
                (Backbuffer, middlebuffer) = (middlebuffer, Backbuffer);
            }
        }

        public FrameBuffer GetNextFrontBuffer()
        {
            lock (middlebuffer)
            {
                (middlebuffer, frontbuffer) = (frontbuffer, middlebuffer);
            }
            return frontbuffer;
        }

        #endregion Render

    }
}