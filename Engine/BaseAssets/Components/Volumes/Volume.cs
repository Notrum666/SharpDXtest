using System.Linq;
using System.Reflection;

using LinearAlgebra;

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Engine.BaseAssets.Components
{
    public abstract class Volume : BehaviourComponent
    {
        [SerializedField]
        protected Vector3f size = new Vector3f(1.0f, 1.0f, 1.0f);
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
                GenerateBoxVertices();
            }
        }
        private Buffer boxVertexBuffer;
        private VertexBufferBinding boxVertexBufferBinding;
        private Buffer boxIndexBuffer;

        protected override void OnInitialized()
        {
            GenerateBoxVertices();
        }
        
        public override void OnFieldChanged(FieldInfo fieldInfo)
        {
            if (fieldInfo.Name == nameof(size))
                Size = size;
        }

        public virtual void Render()
        {
            GraphicsCore.CurrentDevice.ImmediateContext.InputAssembler.SetVertexBuffers(0, boxVertexBufferBinding);
            GraphicsCore.CurrentDevice.ImmediateContext.InputAssembler.SetIndexBuffer(boxIndexBuffer, Format.R32_UInt, 0);
            GraphicsCore.CurrentDevice.ImmediateContext.DrawIndexed(36, 0, 0);
        }

        private void GenerateBoxVertices()
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

            boxVertexBuffer = Buffer.Create(GraphicsCore.CurrentDevice, BindFlags.VertexBuffer, vertexes.ToArray());
            boxVertexBufferBinding = new VertexBufferBinding(boxVertexBuffer, Utilities.SizeOf<Vector3f>(), 0);
            boxIndexBuffer = Buffer.Create(GraphicsCore.CurrentDevice, BindFlags.IndexBuffer, indexes);
        }
    }
}