using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LinearAlgebra;

namespace Engine.BaseAssets.Components.Colliders
{
    public class MeshCollider : Collider
    {
        protected List<Vector3> vertexes = new List<Vector3>();
        protected List<Vector3> normals = new List<Vector3>();
        protected List<int[]> polygons = new List<int[]>();
        public IReadOnlyList<Vector3> Vertexes
        {
            get
            {
                return vertexes.AsReadOnly();
            }
        }
        public IReadOnlyList<Vector3> Normals
        {
            get
            {
                return normals.AsReadOnly();
            }
        }
        public IReadOnlyList<int[]> Polygons
        {
            get
            {
                return polygons.Select(arr => (int[])arr.Clone()).ToList().AsReadOnly();
            }
        }
        private Vector3 inertiaTensor = new Vector3(1.0, 1.0, 1.0);
        public override Vector3 InertiaTensor
        {
            get
            {
                return inertiaTensor;
            }
        }

        private List<Vector3> globalVertexes = new List<Vector3>();
        private List<Vector3> globalNormals = new List<Vector3>();
        public IReadOnlyList<Vector3> GlobalVertexes
        {
            get
            {
                return globalVertexes.AsReadOnly();
            }
        }
        public IReadOnlyList<Vector3> GlobalNormals
        {
            get
            {
                return globalNormals.AsReadOnly();
            }
        }
        private double squaredOuterSphereRadius;
        public override double SquaredOuterSphereRadius
        {
            get
            {
                return squaredOuterSphereRadius;
            }
        }
        private double outerSphereRadius;
        public override double OuterSphereRadius
        {
            get
            {
                return outerSphereRadius;
            }
        }

        private List<Vector3> globalUniqueNormals = new List<Vector3>();
        protected void recalculateOuterSphere()
        {
            double curSqrRadius;
            foreach (Vector3 vertex in vertexes)
            {
                curSqrRadius = vertex.dot(vertex);
                if (curSqrRadius > squaredOuterSphereRadius)
                {
                    squaredOuterSphereRadius = curSqrRadius;
                    outerSphereRadius = Math.Sqrt(curSqrRadius);
                }
            }
        }

        protected override void getBoundaryPointsInDirection(Vector3 direction, out Vector3 hindmost, out Vector3 furthest)
        {
            if (globalVertexes.Count == 0)
                throw new Exception("No vertexes present in this collider.");

            hindmost = Vector3.Zero; 
            furthest = Vector3.Zero;
            double furthestValue = double.MinValue, hindmostValue = double.MaxValue;

            double curValue;
            foreach (Vector3 vertex in globalVertexes)
            {
                curValue = vertex.dot(direction);
                if (curValue > furthestValue)
                {
                    furthest = vertex;
                    furthestValue = curValue;
                }
                if (curValue < hindmostValue)
                {
                    hindmost = vertex;
                    hindmostValue = curValue;
                }
            }
        }

        protected override Vector3[] getPossibleCollisionDirections(Collider other)
        {
            return globalUniqueNormals.ToArray();
        }

        protected override List<Vector3> getVertexesOnPlane(Vector3 collisionPlanePoint, Vector3 collisionPlaneNormal, double epsilon)
        {
            List<Vector3> result = new List<Vector3>();
            foreach (Vector3 vertex in globalVertexes)
                if (Math.Abs((vertex - collisionPlanePoint).dot(collisionPlaneNormal)) <= epsilon * epsilon)
                    result.Add(vertex);
            return result;
        }

        public MeshCollider()
        {

        }
        public override void updateData()
        {
            base.updateData();

            globalVertexes.Clear();
            globalNormals.Clear();
            globalUniqueNormals.Clear();

            Matrix4x4 model = gameObject.transform.Model;
            foreach (Vector3 vertex in vertexes)
                globalVertexes.Add((model * new Vector4(vertex + Offset, 1.0)).xyz);
            bool exists;
            Vector3 globalNormal;
            foreach (Vector3 normal in normals)
            {
                globalNormal = (model * new Vector4(normal, 0.0)).xyz;
                globalNormals.Add(globalNormal);
                exists = false;
                foreach (Vector3 uniqueNormal in globalUniqueNormals)
                    if (uniqueNormal.isCollinearTo(globalNormal))
                    {
                        exists = true;
                        break;
                    }
                if (!exists)
                    globalUniqueNormals.Add(globalNormal);
            }
        }
    }
}
