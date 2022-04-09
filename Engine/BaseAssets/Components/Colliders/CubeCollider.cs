using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LinearAlgebra;

namespace Engine.BaseAssets.Components.Colliders
{
    public class CubeCollider : Collider
    {
        private Vector3 size;
        public Vector3 Size 
        { 
            get
            {
                return size;
            }
            set
            {
                if (value.x <= 0 || value.y <= 0 || value.z <= 0)
                    throw new ArgumentException("Size of collider in all dimensions should be positive!");

                size = value;
                calculateInertiaTensor();
                generateVertices();
                buildPolygons();
            }
        }
        private Vector3 offset;
        public override Vector3 Offset 
        {
            get
            {
                return offset;
            }
            set
            {
                offset = value;
                generateVertices();
                buildPolygons();
            }
        }

        public CubeCollider()
        {
            size = new Vector3(1.0, 1.0, 1.0);
            offset = Vector3.Zero;
        }
        public CubeCollider(Vector3 size)
        {
            Size = size;
            offset = Vector3.Zero;
        }
        public CubeCollider(Vector3 size, Vector3 offset)
        {
            Size = size;
            Offset = offset;
        }

        private void calculateInertiaTensor()
        {
            InertiaTensor = 1.0 / 12.0 * new Vector3(Size.y * Size.y + Size.z * Size.z,
                                                     Size.x * Size.x + Size.z * Size.z,
                                                     Size.x * Size.x + Size.y * Size.y);
        }

        private void generateVertices()
        {
            vertices = new List<Vector3>(8);

            vertices.Add(-0.5 * Size);
            vertices.Add(0.5 * new Vector3(-Size.x, -Size.y, Size.z));

            vertices.Add(0.5 * new Vector3(-Size.x, Size.y, -Size.z));
            vertices.Add(0.5 * new Vector3(-Size.x, Size.y, Size.z));
            
            vertices.Add(0.5 * new Vector3(Size.x, Size.y, -Size.z));
            vertices.Add(0.5 * Size);

            vertices.Add(0.5 * new Vector3(Size.x, -Size.y, -Size.z));
            vertices.Add(0.5 * new Vector3(Size.x, -Size.y, Size.z));

            for (int i = 0; i < 8; i++)
                vertices[i] += Offset;

            calculateOuterSphereRadius();
        }

        private void buildPolygons()
        {
            polygons = new List<int[]>(6);

            polygons.Add(new int[] { 0, 2, 4, 6 }); // Down
            polygons.Add(new int[] { 1, 7, 5, 3 }); // Up

            polygons.Add(new int[] { 6, 7, 1, 0 }); // Back
            polygons.Add(new int[] { 2, 3, 5, 4 }); // Forward

            polygons.Add(new int[] { 0, 1, 3, 2 }); // Left
            polygons.Add(new int[] { 4, 5, 7, 6 }); // Right
        }
    }
}
