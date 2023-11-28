using System;

using LinearAlgebra;

namespace Engine.BaseAssets.Components.Colliders
{
    public class CubeCollider : MeshCollider
    {
        private Vector3 size;
        public Vector3 Size
        {
            get => size;
            set
            {
                if (value.x <= 0 || value.y <= 0 || value.z <= 0)
                    throw new ArgumentException("Size of collider in all dimensions should be positive!");

                size = value;
                calculateInertiaTensor();
                buildCollider();
            }
        }
        private Vector3 inertiaTensor;
        public override Vector3 InertiaTensor => inertiaTensor;

        public CubeCollider()
        {
            Size = new Vector3(1.0, 1.0, 1.0);
            Offset = Vector3.Zero;
        }

        public CubeCollider(Vector3 size)
        {
            Size = size;
            Offset = Vector3.Zero;
        }

        public CubeCollider(Vector3 size, Vector3 offset)
        {
            Size = size;
            Offset = offset;
        }

        private void calculateInertiaTensor()
        {
            inertiaTensor = 1.0 / 12.0 * new Vector3(Size.y * Size.y + Size.z * Size.z,
                                                     Size.x * Size.x + Size.z * Size.z,
                                                     Size.x * Size.x + Size.y * Size.y);
        }

        private void buildCollider()
        {
            generateVertexes();
            generateNormals();
            buildEdges();
            buildPolygons();
        }

        private void generateVertexes()
        {
            vertexes.Clear();

            vertexes.Add(-0.5 * Size);
            vertexes.Add(0.5 * new Vector3(-Size.x, -Size.y, Size.z));

            vertexes.Add(0.5 * new Vector3(-Size.x, Size.y, -Size.z));
            vertexes.Add(0.5 * new Vector3(-Size.x, Size.y, Size.z));

            vertexes.Add(0.5 * new Vector3(Size.x, Size.y, -Size.z));
            vertexes.Add(0.5 * Size);

            vertexes.Add(0.5 * new Vector3(Size.x, -Size.y, -Size.z));
            vertexes.Add(0.5 * new Vector3(Size.x, -Size.y, Size.z));
        }

        private void generateNormals()
        {
            normals.Clear();

            normals.Add(-Vector3.Up);
            normals.Add(Vector3.Up);

            normals.Add(-Vector3.Forward);
            normals.Add(Vector3.Forward);

            normals.Add(-Vector3.Right);
            normals.Add(Vector3.Right);

            nonCollinearNormals.Clear();

            nonCollinearNormals.Add(Vector3.Up);
            nonCollinearNormals.Add(Vector3.Forward);
            nonCollinearNormals.Add(Vector3.Right);
        }

        private void buildPolygons()
        {
            polygons.Clear();

            polygons.Add(new int[] { 0, 2, 4, 6 }); // Down
            polygons.Add(new int[] { 1, 7, 5, 3 }); // Up

            polygons.Add(new int[] { 6, 7, 1, 0 }); // Back
            polygons.Add(new int[] { 2, 3, 5, 4 }); // Forward

            polygons.Add(new int[] { 0, 1, 3, 2 }); // Left
            polygons.Add(new int[] { 4, 5, 7, 6 }); // Right
        }

        private void buildEdges()
        {
            edges.Clear();

            edges.Add((0, 2));
            edges.Add((2, 4));
            edges.Add((4, 6));
            edges.Add((6, 0));
            edges.Add((1, 7));
            edges.Add((7, 5));
            edges.Add((5, 3));
            edges.Add((3, 1));
            edges.Add((6, 7));
            edges.Add((1, 0));
            edges.Add((2, 3));
            edges.Add((5, 4));
        }
    }
}