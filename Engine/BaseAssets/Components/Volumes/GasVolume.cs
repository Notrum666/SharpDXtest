using System.Linq;
using System.Reflection;

using LinearAlgebra;

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Engine.BaseAssets.Components
{
    public sealed class GasVolume : BehaviourComponent
    {
        [SerializedField]
        private Vector3f size = new Vector3f(1.0f, 1.0f, 1.0f);
        public Vector3f Size
        {
            get => size;
            set
            {
                if (value.x <= Constants.Epsilon ||
                    value.y <= Constants.Epsilon ||
                    value.z <= Constants.Epsilon)
                {
                    Logger.Log(LogType.Warning, "Size must be positive");
                    return;
                }
                size = value;
                GenerateVertices();
            }
        }
        [SerializedField]
        private Vector3f lightDirection = -Vector3f.Up;
        public Vector3f LightDirection
        {
            get => lightDirection;
            set
            {
                if (value.isZero())
                {
                    Logger.Log(LogType.Warning, "LightDirection must be non-zero");
                    return;
                }
                lightDirection = value;
            }
        }
        [SerializedField]
        private double absorptionCoef = 0.0;
        public double AbsorptionCoef
        {
            get => absorptionCoef;
            set
            {
                if (value < 0.0 || value > 1.0)
                {
                    Logger.Log(LogType.Warning, "AbsorptionCoef must be in range of 0 to 1");
                    return;
                }
                absorptionCoef = value;
            }
        }
        [SerializedField]
        private double scatteringCoef = 0.03;
        public double ScatteringCoef
        {
            get => scatteringCoef;
            set
            {
                if (value < 0.0 || value > 1.0)
                {
                    Logger.Log(LogType.Warning, "ScatteringCoef must be in range of 0 to 1");
                    return;
                }
                scatteringCoef = value;
            }
        }
        private Buffer vertexBuffer;
        private VertexBufferBinding vertexBufferBinding;
        private Buffer indexBuffer;

        protected override void OnInitialized()
        {
            GenerateVertices();
        }
        
        public override void OnFieldChanged(FieldInfo fieldInfo)
        {
            if (fieldInfo.Name == nameof(size))
                Size = size;
            if (fieldInfo.Name == nameof(scatteringCoef))
                ScatteringCoef = scatteringCoef;
            if (fieldInfo.Name == nameof(absorptionCoef))
                AbsorptionCoef = absorptionCoef;
            if (fieldInfo.Name == nameof(lightDirection))
                LightDirection = lightDirection;
        }

        public void Render()
        {
            GraphicsCore.CurrentDevice.ImmediateContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
            GraphicsCore.CurrentDevice.ImmediateContext.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);
            GraphicsCore.CurrentDevice.ImmediateContext.DrawIndexed(36, 0, 0);
        }

        private void GenerateVertices()
        {
            Vector3f[] vertexes = new Vector3f[8];
            for (int i = 0; i < 8; i++)
                vertexes[i] = new Vector3f((i & 1) == 0 ? -size.x : size.x, (i & 2) == 0 ? -size.y : size.y, (i & 4) == 0 ? -size.z : size.z) * 0.5f;
            int[] indexes = new int[3 * 2 * 6]
            {
                0, 1, 4, 4, 1, 5,
                3, 2, 7, 7, 2, 6,
                2, 0, 6, 6, 0, 4,
                1, 3, 5, 5, 3, 7,
                2, 3, 0, 0, 3, 1,
                4, 5, 6, 6, 5, 7
            };

            vertexBuffer = Buffer.Create(GraphicsCore.CurrentDevice, BindFlags.VertexBuffer, vertexes.ToArray());
            vertexBufferBinding = new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vector3f>(), 0);
            indexBuffer = Buffer.Create(GraphicsCore.CurrentDevice, BindFlags.IndexBuffer, indexes);
        }
    }
}