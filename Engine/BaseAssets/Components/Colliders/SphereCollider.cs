using System;
using System.Collections.Generic;
using System.Reflection;

using LinearAlgebra;

namespace Engine.BaseAssets.Components.Colliders
{
    public sealed class SphereCollider : Collider
    {
        [SerializedField]
        private double radius;

        public Ranged<double> Radius => new Ranged<double>(ref radius, min: 0 + double.Epsilon, onSet: () =>
        {
            RecalculateOuterSphere();
            CalculateInertiaTensor();
        });

        public override void OnFieldChanged(FieldInfo fieldInfo)
        {
            base.OnFieldChanged(fieldInfo);

            switch (fieldInfo.Name)
            {
                case nameof(radius):
                    Radius.Set(radius);
                    return;
            }
        }
        
        internal override void OnDeserialized()
        {
            base.OnDeserialized();
            Radius.Set(radius);
        }

        public override Vector3 InertiaTensor => inertiaTensor;
        public override double OuterSphereRadius => outerSphereRadius;
        public override double SquaredOuterSphereRadius => squaredOuterSphereRadius;

        private Vector3 inertiaTensor = new Vector3(1.0, 1.0, 1.0);
        private double squaredOuterSphereRadius;
        private double outerSphereRadius;

        public SphereCollider()
        {
            Radius.Set(1.0);
        }

        public SphereCollider(double radius)
        {
            Radius.Set(radius);
        }

        public SphereCollider(double radius, Vector3 offset)
        {
            Radius.Set(radius);
            Offset = offset;
        }

        protected override List<Vector3> GetVertexesOnPlane(Vector3 collisionPlanePoint, Vector3 collisionPlaneNormal, double epsilon)
        {
            Vector3 result = (collisionPlanePoint - GlobalCenter).projectOnVector(collisionPlaneNormal);

            if (Math.Abs(result.length() - radius) > epsilon)
                return new List<Vector3>();

            return new List<Vector3>() { GlobalCenter + result };
        }

        protected override void GetBoundaryPointsInDirection(Vector3 direction, out Vector3 hindmost, out Vector3 furthest)
        {
            direction = direction.normalized() * radius;
            furthest = GlobalCenter + direction;
            hindmost = GlobalCenter - direction;
        }

        private void CalculateInertiaTensor()
        {
            double inertia = 2.0 / 5.0 * radius * radius;
            inertiaTensor = new Vector3(inertia, inertia, inertia);
        }

        private void RecalculateOuterSphere()
        {
            outerSphereRadius = radius;
            squaredOuterSphereRadius = radius * radius;
        }
    }
}