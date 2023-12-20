using System;
using System.Collections.Generic;
using LinearAlgebra;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Engine
{
    public class Mesh : BaseAsset //TODO: rename to model?
    {
        private bool disposed;
        public List<Primitive> Primitives { get; } = new List<Primitive>();

        public void Render()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(Mesh));
            foreach (Primitive primitive in Primitives)
                primitive.Render();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                foreach (Primitive primitive in Primitives)
                    primitive.Dispose(disposing);
            }
            disposed = true;

            base.Dispose(disposing);
        }
    }

    public class Primitive : IDisposable //TODO: rename to mesh?
    {
        // material assigned on mesh load
        public Material DefaultMaterial { get; set; } = null;

        public List<PrimitiveVertex> vertices = null;
        public List<int> indices = null;

        private Buffer vertexBuffer;
        private VertexBufferBinding vertexBufferBinding;
        private Buffer indexBuffer;

        private bool disposed;

        public void GenerateGPUData()
        {
            if (vertices == null || indices == null)
                throw new Exception("Geometry data can't be empty.");

            for (int i = 0; i < indices.Count / 3; i++)
            {
                Vector3f edge1 = vertices[i * 3 + 1].v - vertices[i * 3 + 0].v;
                Vector3f edge2 = vertices[i * 3 + 2].v - vertices[i * 3 + 0].v;
                Vector2f UVedge1 = vertices[i * 3 + 1].t - vertices[i * 3 + 0].t;
                Vector2f UVedge2 = vertices[i * 3 + 2].t - vertices[i * 3 + 0].t;
                Vector3f tx = ((edge1 * UVedge2.y - edge2 * UVedge1.y) / (UVedge1.x * UVedge2.y - UVedge1.y * UVedge2.x)).normalized();
                PrimitiveVertex vertex0 = vertices[i * 3 + 0];
                PrimitiveVertex vertex1 = vertices[i * 3 + 1];
                PrimitiveVertex vertex2 = vertices[i * 3 + 2];

                vertex0.tx = new Vector3f(tx.x, tx.y, tx.z);
                vertex1.tx = new Vector3f(tx.x, tx.y, tx.z);
                vertex2.tx = new Vector3f(tx.x, tx.y, tx.z);

                vertices[i * 3 + 0] = vertex0;
                vertices[i * 3 + 1] = vertex1;
                vertices[i * 3 + 2] = vertex2;
            }

            vertexBuffer = Buffer.Create(GraphicsCore.CurrentDevice, BindFlags.VertexBuffer, vertices.ToArray());
            vertexBufferBinding = new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<PrimitiveVertex>(), 0);
            indexBuffer = Buffer.Create(GraphicsCore.CurrentDevice, BindFlags.IndexBuffer, indices.ToArray());
        }

        public void Render()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(Primitive));
            GraphicsCore.CurrentDevice.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            GraphicsCore.CurrentDevice.ImmediateContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
            GraphicsCore.CurrentDevice.ImmediateContext.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);
            GraphicsCore.CurrentDevice.ImmediateContext.DrawIndexed(indices.Count, 0, 0);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                vertices = null;
                indices = null;

                if (vertexBuffer != null)
                    vertexBuffer.Dispose();
                if (indexBuffer != null)
                    indexBuffer.Dispose();
            }

            disposed = true;
        }

        ~Primitive()
        {
            Dispose(disposing: false);
        }

        public struct PrimitiveVertex
        {
            public Vector3f v;
            public Vector2f t;
            public Vector3f n;
            public Vector3f tx;
        }
    }
}