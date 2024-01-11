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
    public class Model : BaseAsset
    {
        private bool disposed;
        public List<Mesh> Meshes { get; } = new List<Mesh>();

        public void Render()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(Model));
            foreach (Mesh mesh in Meshes)
                mesh.Render();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                foreach (Mesh mesh in Meshes)
                    mesh.Dispose(disposing);
            }
            disposed = true;

            base.Dispose(disposing);
        }
    }

    public class Mesh : IDisposable
    {
        // material assigned on mesh load
        public Material DefaultMaterial { get; set; } = null;
        public Skeleton Skeleton { get; set; } = null;

        public readonly List<PrimitiveVertex> Vertices = new List<PrimitiveVertex>();
        public readonly List<int> Indices = new List<int>();

        private Buffer vertexBuffer;
        private VertexBufferBinding vertexBufferBinding;
        private Buffer indexBuffer;

        private bool disposed;

        public void GenerateGPUData()
        {
            if (Vertices.Count == 0 || Indices.Count == 0)
                throw new Exception("Geometry data can't be empty.");

            for (int i = 0; i < Indices.Count / 3; i++)
            {
                Vector3f edge1 = Vertices[i * 3 + 1].v - Vertices[i * 3 + 0].v;
                Vector3f edge2 = Vertices[i * 3 + 2].v - Vertices[i * 3 + 0].v;
                Vector2f UVedge1 = Vertices[i * 3 + 1].t - Vertices[i * 3 + 0].t;
                Vector2f UVedge2 = Vertices[i * 3 + 2].t - Vertices[i * 3 + 0].t;
                Vector3f tx = ((edge1 * UVedge2.y - edge2 * UVedge1.y) / (UVedge1.x * UVedge2.y - UVedge1.y * UVedge2.x)).normalized();
                PrimitiveVertex vertex0 = Vertices[i * 3 + 0];
                PrimitiveVertex vertex1 = Vertices[i * 3 + 1];
                PrimitiveVertex vertex2 = Vertices[i * 3 + 2];

                vertex0.tx = new Vector3f(tx.x, tx.y, tx.z);
                vertex1.tx = new Vector3f(tx.x, tx.y, tx.z);
                vertex2.tx = new Vector3f(tx.x, tx.y, tx.z);

                Vertices[i * 3 + 0] = vertex0;
                Vertices[i * 3 + 1] = vertex1;
                Vertices[i * 3 + 2] = vertex2;
            }

            vertexBuffer = Buffer.Create(GraphicsCore.CurrentDevice, BindFlags.VertexBuffer, Vertices.ToArray());
            vertexBufferBinding = new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<PrimitiveVertex>(), 0);
            indexBuffer = Buffer.Create(GraphicsCore.CurrentDevice, BindFlags.IndexBuffer, Indices.ToArray());
        }

        public void Render()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(Mesh));
            GraphicsCore.CurrentDevice.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            GraphicsCore.CurrentDevice.ImmediateContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
            GraphicsCore.CurrentDevice.ImmediateContext.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);
            GraphicsCore.CurrentDevice.ImmediateContext.DrawIndexed(Indices.Count, 0, 0);
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
                Vertices.Clear();
                Indices.Clear();

                if (vertexBuffer != null)
                    vertexBuffer.Dispose();
                if (indexBuffer != null)
                    indexBuffer.Dispose();
            }

            disposed = true;
        }

        ~Mesh()
        {
            Dispose(disposing: false);
        }

        public struct PrimitiveVertex
        {
            public Vector3f v;
            public Vector2f t;
            public Vector3f n;
            public Vector3f tx;
            public Vector4i bones;
            public Vector4f weights;
        }
    }
}