using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LinearAlgebra;
//using SharpDX;

namespace Engine.BaseAssets.Components.Colliders
{
    public class MeshCollider : Collider
    {
        public Model Model 
        { 
            set
            {
                FromModel(value);
            } 
        }

        protected List<Vector3> vertexes = new List<Vector3>();
        protected List<Vector3> normals = new List<Vector3>();
        protected List<Vector3> nonCollinearNormals = new List<Vector3>();
        protected List<int[]> polygons = new List<int[]>();
        protected List<(int a, int b)> edges = new List<(int a, int b)>();
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
        public IReadOnlyList<Vector3> NonCollinearNormals
        {
            get
            {
                return nonCollinearNormals.AsReadOnly();
            }
        }
        public IReadOnlyList<int[]> Polygons
        {
            get
            {
                return polygons.Select(arr => (int[])arr.Clone()).ToList().AsReadOnly();
            }
        }
        public IReadOnlyList<(int a, int b)> Edges
        {
            get
            {
                return edges.AsReadOnly();
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
        private List<Vector3> globalNonCollinearNormals = new List<Vector3>();
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
        public IReadOnlyList<Vector3> GlobalNonCollinearNormals
        {
            get
            {
                return globalNonCollinearNormals.AsReadOnly();
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
        public void FromModel(Model model)
        {
            vertexes = new List<Vector3>(model.v);
            polygons = new List<int[]>(model.v_i);

            normals = new List<Vector3>(polygons.Count);
            edges = new List<(int a, int b)>();
            int a, b;
            bool exists;
            foreach (int[] poly in polygons)
            {
                normals.Add((vertexes[poly[1]] - vertexes[poly[0]]).cross(vertexes[poly[2]] - vertexes[poly[0]]));
                for (int i = 0; i < poly.Length; i++)
                {
                    a = poly[i];
                    b = poly[(i + 1) % poly.Length];

                    exists = false;
                    foreach ((int a, int b) edge in edges)
                        if (a == edge.a && b == edge.b || a == edge.b && b == edge.a)
                        {
                            exists = true;
                            break;
                        }

                    if (!exists)
                        edges.Add((a, b));
                }
            }

            void addUnique(Vector3 vec)
            {
                exists = false;
                foreach (Vector3 vector in nonCollinearNormals)
                    if (vector.isCollinearTo(vec))
                    {
                        exists = true;
                        break;
                    }
                if (!exists)
                    nonCollinearNormals.Add(vec);
            }

            foreach (Vector3 normal in normals)
                addUnique(normal);
        }
        public override void updateData()
        {
            base.updateData();

            globalVertexes.Clear();
            globalNormals.Clear();
            globalNonCollinearNormals.Clear();

            Matrix4x4 model = GameObject.Transform.Model;
            foreach (Vector3 vertex in vertexes)
                globalVertexes.Add(model.TransformPoint(vertex + Offset));
            foreach (Vector3 normal in normals)
                globalNormals.Add(model.TransformDirection(normal));
            foreach (Vector3 normal in nonCollinearNormals)
                globalNonCollinearNormals.Add(model.TransformDirection(normal));

            double curSqrRadius;
            foreach (Vector3 vertex in globalVertexes)
            {
                curSqrRadius = (vertex - GlobalCenter).squaredLength();
                if (curSqrRadius > squaredOuterSphereRadius)
                    squaredOuterSphereRadius = curSqrRadius;
            }
            outerSphereRadius = Math.Sqrt(squaredOuterSphereRadius);
        }
    }
}
