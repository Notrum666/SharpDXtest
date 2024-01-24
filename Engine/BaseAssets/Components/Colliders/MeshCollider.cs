using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using LinearAlgebra;
//using SharpDX;

namespace Engine.BaseAssets.Components.Colliders
{
    public class MeshCollider : Collider
    {
        [SerializedField]
        private Model model;

        public Model Model
        {
            get => model;
            set
            {
                model = value;
                OnModelUpdated();
            }
        }

        public override void OnFieldChanged(FieldInfo fieldInfo)
        {
            base.OnFieldChanged(fieldInfo);

            switch (fieldInfo.Name)
            {
                case nameof(model):
                    Model = model;
                    return;
            }
        }

        internal override void OnDeserialized()
        {
            base.OnDeserialized();
            Model = model;
        }

        public override Vector3 InertiaTensor => inertiaTensor;
        public override double OuterSphereRadius => outerSphereRadius;
        public override double SquaredOuterSphereRadius => squaredOuterSphereRadius;

        private Vector3 inertiaTensor = new Vector3(1.0, 1.0, 1.0);
        private double outerSphereRadius;
        private double squaredOuterSphereRadius;

        public IReadOnlyList<Vector3> Vertices => vertices.AsReadOnly();
        public IReadOnlyList<Vector3> Normals => normals.AsReadOnly();
        public IReadOnlyList<Vector3> NonCollinearNormals => nonCollinearNormals.AsReadOnly();
        public IReadOnlyList<int[]> Polygons => polygons.Select(arr => (int[])arr.Clone()).ToList().AsReadOnly();
        public IReadOnlyList<(int a, int b)> Edges => edges.AsReadOnly();

        protected List<Vector3> vertices = new List<Vector3>();
        protected List<Vector3> normals = new List<Vector3>();
        protected List<Vector3> nonCollinearNormals = new List<Vector3>();
        protected List<int[]> polygons = new List<int[]>();
        protected List<(int a, int b)> edges = new List<(int a, int b)>();

        public IReadOnlyList<Vector3> GlobalVertexes => globalVertexes.AsReadOnly();
        public IReadOnlyList<Vector3> GlobalNormals => globalNormals.AsReadOnly();
        public IReadOnlyList<Vector3> GlobalNonCollinearNormals => globalNonCollinearNormals.AsReadOnly();

        private List<Vector3> globalVertexes = new List<Vector3>();
        private List<Vector3> globalNormals = new List<Vector3>();
        private List<Vector3> globalNonCollinearNormals = new List<Vector3>();

        internal override void UpdateData()
        {
            base.UpdateData();

            globalVertexes.Clear();
            globalNormals.Clear();
            globalNonCollinearNormals.Clear();

            Matrix4x4 model = GameObject.Transform.Model;
            foreach (Vector3 vertex in vertices)
                globalVertexes.Add(model.TransformPoint(vertex + Offset));
            foreach (Vector3 normal in normals)
                globalNormals.Add(model.TransformDirection(normal));
            foreach (Vector3 normal in nonCollinearNormals)
                globalNonCollinearNormals.Add(model.TransformDirection(normal));

            foreach (Vector3 vertex in globalVertexes)
            {
                var curSqrRadius = (vertex - GlobalCenter).squaredLength();
                if (curSqrRadius > squaredOuterSphereRadius)
                    squaredOuterSphereRadius = curSqrRadius;
            }
            outerSphereRadius = Math.Sqrt(squaredOuterSphereRadius);
        }

        protected override List<Vector3> GetVertexesOnPlane(Vector3 collisionPlanePoint, Vector3 collisionPlaneNormal, double epsilon)
        {
            List<Vector3> result = new List<Vector3>();
            foreach (Vector3 vertex in globalVertexes)
            {
                if (Math.Abs((vertex - collisionPlanePoint).dot(collisionPlaneNormal)) <= epsilon * epsilon)
                    result.Add(vertex);
            }
            return result;
        }

        protected override void GetBoundaryPointsInDirection(Vector3 direction, out Vector3 hindmost, out Vector3 furthest)
        {
            if (globalVertexes.Count == 0)
            {
                Logger.Log(LogType.Error, "Tried to get boundary points of collider with no vertices");
                hindmost = Vector3.Zero;
                furthest = Vector3.Zero;
                return;
            }

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

        private void OnModelUpdated()
        {
            if (model == null)
            {
                vertices.Clear();
                normals.Clear();
                nonCollinearNormals.Clear();
                polygons.Clear();
                edges.Clear();

                globalVertexes.Clear();
                globalNormals.Clear();
                globalNonCollinearNormals.Clear();
                return;
            }

            vertices = new List<Vector3>();
            polygons = new List<int[]>();
            foreach (Mesh mesh in model.Meshes)
            {
                for (int i = 0; i < mesh.Indices.Count;)
                {
                    if (mesh.Indices[i] == -1) // restart index
                    {
                        ++i;
                        continue;
                    }
                    int[] polygonIndices =
                    {
                        mesh.Indices[i] + vertices.Count,
                        mesh.Indices[i + 1] + vertices.Count,
                        mesh.Indices[i + 2] + vertices.Count
                    };
                    polygons.Add(polygonIndices);
                    i += 3;
                }
                foreach (Mesh.PrimitiveVertex vertex in mesh.Vertices)
                    vertices.Add(vertex.v);
            }

            normals = new List<Vector3>(polygons.Count);
            edges = new List<(int a, int b)>();
            int a, b;
            bool exists;
            foreach (int[] poly in polygons)
            {
                normals.Add((vertices[poly[1]] - vertices[poly[0]]).cross(vertices[poly[2]] - vertices[poly[0]]));
                for (int i = 0; i < poly.Length; i++)
                {
                    a = poly[i];
                    b = poly[(i + 1) % poly.Length];

                    exists = false;
                    foreach ((int a, int b) edge in edges)
                    {
                        if (a == edge.a && b == edge.b || a == edge.b && b == edge.a)
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                        edges.Add((a, b));
                }
            }

            void AddUnique(Vector3 vec)
            {
                exists = false;
                foreach (Vector3 vector in nonCollinearNormals)
                {
                    if (vector.isCollinearTo(vec))
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                    nonCollinearNormals.Add(vec);
            }

            foreach (Vector3 normal in normals)
                AddUnique(normal);
        }
    }
}