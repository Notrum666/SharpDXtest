using Engine.BaseAssets.Components;
using LinearAlgebra;

namespace Engine
{
    public struct Ray
    {
        public Vector3 Origin;
        public Vector3 Direction;

        public Ray(Vector3 origin, Vector3 direction)
        {
            Origin = origin;
            Direction = direction;
        }
    }

    public struct HitResult
    {
        public Vector3 Point;
        public GameObject Target;
    }

    public class Raycast
    {
        public static bool HitMesh(Ray ray, out HitResult hitResult)
        {
            hitResult = default;
            bool hasHitResult = false;

            foreach(MeshComponent meshComponent in Scene.FindComponentsOfType<MeshComponent>())
            {
                var model = meshComponent.Model;

                if (!meshComponent.LocalEnabled || model == null)
                    continue;

                GameObject gameObject = meshComponent.GameObject;

                if (!IsIntersectBoundingSphere(ray, meshComponent))
                    continue;

                foreach (Mesh mesh in model.Meshes)
                {
                    for (int i = 0; i < mesh.Indices.Count; i += 3)
                    {
                        Vector3 v0 = mesh.Vertices[mesh.Indices[i + 0]].v;
                        Vector3 v1 = mesh.Vertices[mesh.Indices[i + 1]].v;
                        Vector3 v2 = mesh.Vertices[mesh.Indices[i + 2]].v;

                        //TODO: calculate intersection in local model space
                        v0 = gameObject.Transform.Model.TransformPoint(v0);
                        v1 = gameObject.Transform.Model.TransformPoint(v1);
                        v2 = gameObject.Transform.Model.TransformPoint(v2);

                        Vector3? hit = IntersectRayTriangle(ray.Origin, ray.Direction, v0, v1, v2);

                        if (hit.HasValue)
                        {
                            bool isCurrentResultNearest =
                                hasHitResult
                                && (ray.Origin - hit.Value).squaredLength() 
                                < (ray.Origin - hitResult.Point).squaredLength();

                            if(!hasHitResult || isCurrentResultNearest)
                            {
                                hitResult = new HitResult
                                {
                                    Point = hit.Value,
                                    Target = gameObject
                                };

                                hasHitResult = true;
                            }
                        }
                    }
                }
            }

            return hasHitResult;
        }

        private static bool IsIntersectBoundingSphere(Ray ray, MeshComponent meshComponent)
        {
            Vector3 center = meshComponent.GameObject.Transform.Position;

            Vector3 m = ray.Origin - center;
            double b = m.dot(ray.Direction);
            double c = m.dot(m) - meshComponent.SquaredBoundingSphereRadius;

            if (c > 0.0 && b > 0.0)
                return false;

            double discr = b * b - c;

            if (discr < 0.0f)
                return false;

            return true;
        }

        private static Vector3? IntersectRayTriangle(Vector3 rayOrigin, Vector3 rayDirection, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            double eps = 0.0001;

            var edge1 = v1 - v0;
            var edge2 = v2 - v0;

            var pvec = rayDirection.cross(edge2);

            var det = edge1.dot(pvec);

            if (det > -eps && det < eps)
            {
                return null;
            }

            var invDet = 1d / det;

            var tvec = rayOrigin - v0;

            var u = tvec.dot(pvec) * invDet;

            if (u < 0 || u > 1)
            {
                return null;
            }

            var qvec = tvec.cross(edge1);

            var v = rayDirection.dot(qvec) * invDet;

            if (v < 0 || u + v > 1)
            {
                return null;
            }

            var t = edge2.dot(qvec) * invDet;

            return rayOrigin + rayDirection * t;
        }
    }
}
