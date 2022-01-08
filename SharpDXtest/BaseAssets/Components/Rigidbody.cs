using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpDXtest.BaseAssets.Components
{
    class Rigidbody : Component
    {
        private double mass = 1.0;
        public double Mass 
        { 
            get 
            { 
                return mass; 
            } 
            set 
            {
                if (value <= 0)
                    throw new ArgumentException("Mass must be positive.");
                mass = value;
            } 
        }
        public Vector3 InertiaTensor { get; private set; } = new Vector3(1.0, 1.0, 1.0);
        public Vector3 Velocity { get; set; } = Vector3.Zero;
        public Vector3 AngularVelocity { get; set; } = Vector3.Zero;
        private double linearDrag = 0.05;
        public double LinearDrag
        {
            get
            {
                return linearDrag;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentException("Linear drag can't be negative.");
                linearDrag = value;
            }
        }
        private double angularDrag = 0.05;
        public double AngularDrag
        {
            get
            {
                return angularDrag;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentException("Angular drag can't be negative.");
                angularDrag = value;
            }
        }
        public bool IsStatic { get; set; } = false;
        public PhysicalMaterial Material { get; set; }

        public override void fixedUpdate()
        {
            Transform t = gameObject.transform;

            Velocity *= 1.0 - Time.DeltaTime * linearDrag;
            AngularVelocity *= 1.0 - Time.DeltaTime * angularDrag;

            if (t.Parent == null)
            {
                t.localPosition += Velocity * Time.DeltaTime;

                t.localRotation = (t.localRotation +
                    0.5 * new Quaternion(0.0, AngularVelocity.x, AngularVelocity.y, AngularVelocity.z) * t.localRotation * Time.DeltaTime).normalized();
            }
            else
            {
                t.localPosition += (t.Parent.view * new Vector4(Velocity * Time.DeltaTime, 0.0)).xyz;

                // faster
                Vector3 localAngularVelocity = (t.Parent.view * new Vector4(AngularVelocity, 0.0)).xyz;
                t.localRotation = (t.localRotation +
                    0.5 * new Quaternion(0.0, localAngularVelocity.x, localAngularVelocity.y, localAngularVelocity.z) * 
                    t.localRotation * Time.DeltaTime).normalized();
                // simpler
                //t.Rotation = (t.Rotation +
                //    0.5 * new Quaternion(0.0, angularVelocity.x, angularVelocity.y, angularVelocity.z) *
                //    t.Rotation * Time.DeltaTime).normalized();
            }
        }
        private Matrix3x3 getGlobalInertiaTensor()
        {
            Matrix3x3 model = Matrix3x3.FromQuaternion(gameObject.transform.Rotation);
            return model * new Matrix3x3(1.0 / InertiaTensor.x, 0.0, 0.0,
                                         0.0, 1.0 / InertiaTensor.y, 0.0,
                                         0.0, 0.0, 1.0 / InertiaTensor.z) * model.transposed();
        }
        public void addForce(Vector3 force)
        {
            Velocity += force * Time.DeltaTime / mass;
        }
        public void addImpulse(Vector3 impulse)
        {
            Velocity += impulse / mass;
        }
        public void addTorque(Vector3 torque)
        {

            AngularVelocity += torque * getGlobalInertiaTensor() * Time.DeltaTime;
        }
        public void addAngularImpulse(Vector3 angularImpulse)
        {
            AngularVelocity += angularImpulse * getGlobalInertiaTensor();
        }
        public void addForceAtPoint(Vector3 force, Vector3 point)
        {
            Transform t = gameObject.transform;
            Vector3 realPosition = t.Position;
            Vector3 radiusVector = point - realPosition;
            
            Velocity += force.projectOnVector(radiusVector) * Time.DeltaTime / mass;

            AngularVelocity += (radiusVector % force) * getGlobalInertiaTensor() * Time.DeltaTime;
        }
        public void addImpulseAtPoint(Vector3 impulse, Vector3 point)
        {
            Transform t = gameObject.transform;
            Vector3 realPosition = t.Position;
            Vector3 radiusVector = point - realPosition;

            Velocity += impulse.projectOnVector(radiusVector) / mass;

            AngularVelocity += (radiusVector % impulse) * getGlobalInertiaTensor();
        }
        public void solveCollisionWith(Rigidbody otherRigidbody)
        {
            if (!Enabled || !otherRigidbody.Enabled || IsStatic && otherRigidbody.IsStatic)
                return;

            Collider[] colliders = gameObject.getComponents<Collider>();
            Collider[] otherColliders = otherRigidbody.gameObject.getComponents<Collider>();

            foreach (Collider collider in colliders)
                foreach (Collider otherCollider in otherColliders)
                {
                    Vector3? _collisionExitVector;
                    Vector3? _collisionExitNormal;
                    Vector3? _colliderEndPoint;
                    if (!collider.getCollisionExitVector(otherCollider, out _collisionExitVector, out _collisionExitNormal, out _colliderEndPoint))
                        continue;
                    Vector3 collisionExitVector = (Vector3)_collisionExitVector;
                    Vector3 collisionExitNormal = (Vector3)_collisionExitNormal;
                    Vector3 colliderEndPoint = (Vector3)_colliderEndPoint;
                    collisionExitNormal.normalize();

                    if (IsStatic)
                        otherRigidbody.gameObject.transform.Position -= collisionExitVector;
                    else
                    {
                        if (otherRigidbody.IsStatic)
                        {
                            gameObject.transform.Position += collisionExitVector;
                            colliderEndPoint += collisionExitVector;
                        }
                        else
                        {
                            double totalMass = mass + otherRigidbody.mass;
                            gameObject.transform.Position += collisionExitVector * otherRigidbody.mass / totalMass;
                            otherRigidbody.gameObject.transform.Position += collisionExitVector * mass / totalMass;
                        }
                    }

                    Vector3 collisionPoint = Collider.GetAverageCollisionPoint(collider, otherCollider, colliderEndPoint, collisionExitNormal);

                    double denominator = 0.0;
                    if (!IsStatic)
                    {
                        denominator += 1.0 / mass;
                        Vector3 r = collisionPoint - gameObject.transform.Position;
                        denominator += collisionExitNormal * (r % collisionExitNormal * getGlobalInertiaTensor() % r);
                    }
                    if (!otherRigidbody.IsStatic)
                    {
                        denominator += 1.0 / otherRigidbody.mass;
                        Vector3 r = collisionPoint - otherRigidbody.gameObject.transform.Position;
                        denominator += collisionExitNormal * (r % collisionExitNormal * otherRigidbody.getGlobalInertiaTensor() % r);
                    }

                    Vector3 impulse = (otherRigidbody.Velocity - Velocity) / denominator;

                    Vector3 linearImpulse = impulse.projectOnVector(collisionExitNormal) * (1.0 + Material.GetComdinedBouncinessWith(otherRigidbody.Material)) / 2.0;
                    Vector3 angularImpulse = impulse.projectOnFlat(collisionExitNormal) * (1.0 + Material.GetCombinedFrictionWith(otherRigidbody.Material)) / 2.0;

                    impulse = linearImpulse + angularImpulse;

                    addImpulseAtPoint(impulse, collisionPoint);
                    otherRigidbody.addImpulseAtPoint(-impulse, collisionPoint);
                }
        }
    }
}
