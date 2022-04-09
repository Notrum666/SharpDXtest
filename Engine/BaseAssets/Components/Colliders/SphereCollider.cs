using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LinearAlgebra;

namespace Engine.BaseAssets.Components.Colliders
{
    public class SphereCollider : Collider
    {
        private double radius;
        public double Radius 
        {
            get
            {
                return radius;
            }
            set
            {
                if(value <= 0)
                    throw new ArgumentException("Sphere radius should be positive.");

                if (value != radius)
                {
                    radius = value;
                    calculateInertiaTensor();

                    outerSphereRadius = radius;
                    squaredOuterSphereRadius = radius * radius;
                }
            }
        }

        public override Vector3 Offset { get; set; }

        protected Vector3 center;

        public SphereCollider()
        {
            Offset = Vector3.Zero;
            Radius = 1;

            vertices.Add(Vector3.Zero);
            vertices.Add(Vector3.Zero);

            globalSpaceVertices = vertices;
        }

        private void calculateInertiaTensor()
        {
            double inertia = 2.0 / 5.0 * radius * radius;
            InertiaTensor = new Vector3(inertia, inertia, inertia);
        }

        protected override void projectOnVector(Vector3 vector, Vector3[] projection)
        {
            Vector3 normalized = vector.normalized();
            projection[0] = center.projectOnVector(vector) - radius * normalized;
            projection[1] = center.projectOnVector(vector) + radius * normalized;
        }

        public override bool getCollisionExitVector(Collider collider, out Vector3? collisionExitVector, out Vector3? exitDirectionVector, out Vector3? colliderEndPoint)
        {
            collisionExitVector = null;
            exitDirectionVector = null;
            colliderEndPoint = null;

            Vector3 centersVector;
            if (!IsOuterSphereIntersectWith(collider, out centersVector))
                return false;

            if (collider is SphereCollider)
            {
                SphereCollider otherCollider = collider as SphereCollider;

                Vector3 centersVectorNormalized = centersVector.normalized();
                Vector3 r1 = Radius * centersVectorNormalized;
                Vector3 r2 = -otherCollider.Radius * centersVectorNormalized;

                collisionExitVector = centersVector + r2 - r1;
                exitDirectionVector = centersVectorNormalized;
                colliderEndPoint = center + r1;

                return true;
            }

            return base.getCollisionExitVector(collider, out collisionExitVector, out exitDirectionVector, out colliderEndPoint);
        }

        protected override List<int> getVertexOnPlaneIndices(Vector3 collisionPlanePoint, Vector3 collisionPlaneNormal, double epsilon)
        {
            globalSpaceVertices[0] = center + radius * Math.Sign(collisionPlaneNormal.dot(collisionPlanePoint - center)) * collisionPlaneNormal;

            List<int> indices = new List<int>();
            indices.Add(0);

            return indices;
        }

        public override void calculateGlobalVertices()
        {
            
        }

        public override void fixedUpdate()
        {
            center = getCenterInGlobal();
        }
    }
}