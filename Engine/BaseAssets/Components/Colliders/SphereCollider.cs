using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinearAlgebra;

namespace Engine.BaseAssets.Components.Colliders
{
    public sealed class SphereCollider : Collider
    {
        private double radius;
        public double Radius
        {
            get => radius;
            set
            {
                if (value <= 0)
                    throw new ArgumentException("Sphere radius should be positive.");

                radius = value;

                recalculateOuterSphere();
                calculateIntertiaTensor();
            }
        }
        private Vector3 inertiaTensor = new Vector3(1.0, 1.0, 1.0);
        public override Vector3 InertiaTensor => inertiaTensor;
        private double squaredOuterSphereRadius;
        public override double SquaredOuterSphereRadius => squaredOuterSphereRadius;
        private double outerSphereRadius;
        public override double OuterSphereRadius => outerSphereRadius;

        public SphereCollider()
        {
            Radius = 1.0;
        }

        public SphereCollider(double radius)
        {
            Radius = radius;
        }

        public SphereCollider(double radius, Vector3 offset)
        {
            Radius = radius;
            Offset = offset;
        }

        private void calculateIntertiaTensor()
        {
            double inertia = 2.0 / 5.0 * radius * radius;
            inertiaTensor = new Vector3(inertia, inertia, inertia);
        }

        private void recalculateOuterSphere()
        {
            outerSphereRadius = radius;
            squaredOuterSphereRadius = radius * radius;
        }

        protected override void getBoundaryPointsInDirection(Vector3 direction, out Vector3 hindmost, out Vector3 furthest)
        {
            direction = direction.normalized() * radius;
            furthest = GlobalCenter + direction;
            hindmost = GlobalCenter - direction;
        }

        protected override List<Vector3> getVertexesOnPlane(Vector3 collisionPlanePoint, Vector3 collisionPlaneNormal, double epsilon)
        {
            Vector3 result = (collisionPlanePoint - GlobalCenter).projectOnVector(collisionPlaneNormal);

            if (Math.Abs(result.length() - radius) > epsilon)
                return new List<Vector3>();

            return new List<Vector3>() { GlobalCenter + result };
        }
    }
}