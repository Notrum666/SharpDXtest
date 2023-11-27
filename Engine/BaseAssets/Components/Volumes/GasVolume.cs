using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;
using LinearAlgebra;
using Buffer = SharpDX.Direct3D11.Buffer;
using SharpDX;
using SharpDX.DXGI;

namespace Engine.BaseAssets.Components
{
    public sealed class GasVolume : Component
    {
        private Vector3f size = new Vector3f(1.0f, 1.0f, 1.0f);
        public Vector3f Size
        {
            get => size;
            set
            {
                size = value;
                GenerateVertexes();
            }
        }
        public double AbsorptionCoef { get; set; } = 0.0;
        public double ScatteringCoef { get; set; } = 0.03;
        private Buffer vertexBuffer;
        private VertexBufferBinding vertexBufferBinding;
        private Buffer indexBuffer;

        public GasVolume()
        {
            Size = new Vector3f(1.0f, 1.0f, 1.0f);
        }

        public void Render()
        {
            GraphicsCore.CurrentDevice.ImmediateContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
            GraphicsCore.CurrentDevice.ImmediateContext.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);
            GraphicsCore.CurrentDevice.ImmediateContext.DrawIndexed(36, 0, 0);
        }

        private void GenerateVertexes()
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