using Engine.BaseAssets.Components;
using LinearAlgebra;

using Windows.Devices.Radios;

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
        public Vector3 HitPoint;
        public GameObject HitObject;
    }

    public class Raycast
    {
        public static bool HitMesh(in Ray ray, out HitResult hitResult)
        {
            hitResult = new HitResult() { HitPoint = Vector3.Zero, HitObject = null };
            double maxSqrDistance = double.PositiveInfinity;

            foreach(MeshComponent meshComponent in Scene.FindComponentsOfType<MeshComponent>())
            {
                Model model = meshComponent.Model;

                if (!meshComponent.LocalEnabled || model == null)
                    continue;

                GameObject gameObject = meshComponent.GameObject;

                if (!IsIntersectBoundingSphere(ray, meshComponent))
                    continue;

                Vector3 localOrigin = gameObject.Transform.View.TransformPoint(ray.Origin);
                Vector3 localDirection = gameObject.Transform.View.TransformDirection(ray.Direction).normalized();

                foreach (Mesh mesh in model.Meshes)
                {
                    for (int i = 0; i < mesh.Indices.Count; i += 3)
                    {
                        Vector3 v0 = mesh.Vertices[mesh.Indices[i + 0]].v;
                        Vector3 v1 = mesh.Vertices[mesh.Indices[i + 1]].v;
                        Vector3 v2 = mesh.Vertices[mesh.Indices[i + 2]].v;

                        Vector3? hitPoint = IntersectRayTriangle(localOrigin, localDirection, v0, v1, v2);

                        if (hitPoint.HasValue)
                        {
                            double curSqrDistance = (hitPoint.Value - localOrigin).squaredMagnitude();

                            if(curSqrDistance < maxSqrDistance)
                            {
                                hitResult = new HitResult
                                {
                                    HitPoint = hitPoint.Value,
                                    HitObject = gameObject
                                };

                                maxSqrDistance = curSqrDistance;
                            }
                        }
                    }
                }
            }

            if (hitResult.HitObject is null)
                return false;

            hitResult.HitPoint = hitResult.HitObject.Transform.Model.TransformPoint(hitResult.HitPoint);
            return true;
        }

        private static bool IsIntersectBoundingSphere(in Ray ray, MeshComponent meshComponent)
        {
            Vector3 oc = meshComponent.GameObject.Transform.Position - ray.Origin;
            if (oc.squaredLength() <= meshComponent.SquaredBoundingSphereRadius)
                return true;
            if (oc * ray.Direction < 0)
                return false;
            return oc.projectOnFlat(ray.Direction).squaredMagnitude() <= meshComponent.SquaredBoundingSphereRadius;
        }

        private static Vector3? IntersectRayTriangle(in Vector3 rayOrigin, in Vector3 rayDirection, in Vector3 v0, in Vector3 v1, in Vector3 v2)
        {
            // Möller–Trumbore intersection algorithm

            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            Vector3 ray_cross_e2 = rayDirection.cross(edge2);
            double det = edge1 * ray_cross_e2;
            
            if (det >= -Constants.Epsilon && det <= Constants.Epsilon)
                return null; // ray is parallel to the triangle.
            
            double inv_det = 1.0 / det;
            Vector3 s = rayOrigin - v0;
            // calculating first barycentric coordinate
            double u = inv_det * s * ray_cross_e2;
            
            if (u < 0 || u > 1)
                return null; // point lies outside the triangle
            
            Vector3 s_cross_e1 = s.cross(edge1);
            // calculating second barycentric coordinate
            double v = inv_det * rayDirection * s_cross_e1;
            
            if (v < 0 || u + v > 1)
                return null; // point lies outside the triangle
            
            // at this stage we can compute t to find out where the intersection point is on the line.
            double t = inv_det * edge2 * s_cross_e1;
            
            if (t <= Constants.Epsilon) // intersection behind origin point
                return null;
            
            return rayOrigin + rayDirection * t;

            // native implementation

            //Vector3 edge1 = v1 - v0;
            //Vector3 edge2 = v2 - v1;
            //Vector3 normal = edge1.cross(edge2);
            //// if ray is parallel to the polygon - consider it as not intersecting in any case
            //double denominator = normal * rayDirection;
            //if (denominator >= -Constants.Epsilon)
            //    return null;
            //
            //double invLength = 1.0 / normal.length();
            //normal *= invLength;
            //denominator *= invLength;
            //
            //double t = -(normal * (rayOrigin - v0) / denominator);
            //Vector3 point = rayOrigin + t * rayDirection;
            //Vector3 edge3 = v0 - v2;
            //// assumes counter-clockwise winding of vertices (which should be always correct for models)
            //if (edge1.cross(point - v0) * normal < -Constants.Epsilon ||
            //    edge2.cross(point - v1) * normal < -Constants.Epsilon ||
            //    edge3.cross(point - v2) * normal < -Constants.Epsilon)
            //    return null;
            //
            //return point;
        }
    }
}
