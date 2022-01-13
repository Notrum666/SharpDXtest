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
        private List<Vector3> collisionExitVectors = new List<Vector3>();

        public override void fixedUpdate()
        {
            recalculateInertiaTensor();

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
        private void recalculateInertiaTensor()
        {
            Collider[] colliders = gameObject.getComponents<Collider>();
            if (colliders.Length > 0)
                InertiaTensor = colliders[0].InertiaTensor * mass;
        }
        private Matrix3x3 getInverseGlobalInertiaTensor()
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

            AngularVelocity += torque * getInverseGlobalInertiaTensor() * Time.DeltaTime;
        }
        public void addAngularImpulse(Vector3 angularImpulse)
        {
            AngularVelocity += angularImpulse * getInverseGlobalInertiaTensor();
        }
        public void addForceAtPoint(Vector3 force, Vector3 point)
        {
            Velocity += force * Time.DeltaTime / mass;

            AngularVelocity += (point - gameObject.transform.Position) % force * getInverseGlobalInertiaTensor() * Time.DeltaTime;
        }
        public void addImpulseAtPoint(Vector3 impulse, Vector3 point)
        {
            Velocity += impulse / mass;

            AngularVelocity += (point - gameObject.transform.Position) % impulse * getInverseGlobalInertiaTensor();
        }
        public void applyCollisionExitVectors()
        {
            int count = collisionExitVectors.Count();
            if (count == 0)
                return;
            Vector3 result = collisionExitVectors[0];
            for (int i = 1; i < count; i++)
                result += collisionExitVectors[i];
            result /= count;
            gameObject.transform.Position += result;
            collisionExitVectors.Clear();
        }
        public void solveCollisionWith(Rigidbody otherRigidbody)
        {
            if (!Enabled || !otherRigidbody.Enabled || IsStatic && otherRigidbody.IsStatic)
                return;

            Collider[] colliders = gameObject.getComponents<Collider>();
            Collider[] otherColliders = otherRigidbody.gameObject.getComponents<Collider>();

            double systemEnergy = mass * Velocity * Velocity / 2.0 + AngularVelocity * getInverseGlobalInertiaTensor() * AngularVelocity / 2.0 +
                otherRigidbody.mass * otherRigidbody.Velocity * otherRigidbody.Velocity / 2.0 + otherRigidbody.AngularVelocity * otherRigidbody.getInverseGlobalInertiaTensor() * otherRigidbody.AngularVelocity / 2.0;

            foreach (Collider collider in colliders)
                foreach (Collider otherCollider in otherColliders)
                {
                    Vector3? _collisionExitVector;
                    Vector3? _collisionExitNormal;
                    Vector3? _colliderEndPoint;
                    if (!collider.getCollisionExitVector(otherCollider, out _collisionExitVector, out _collisionExitNormal, out _colliderEndPoint))
                        continue;
                    //collider.getCollisionExitVector(otherCollider, out _collisionExitVector, out _collisionExitNormal, out _colliderEndPoint);
                    Vector3 collisionExitVector = (Vector3)_collisionExitVector;
                    Vector3 collisionExitNormal = (Vector3)_collisionExitNormal;
                    Vector3 colliderEndPoint = (Vector3)_colliderEndPoint;
                    collisionExitNormal.normalize();

                    Vector3 moveVector, otherMoveVector;
                    if (IsStatic)
                    {
                        moveVector = Vector3.Zero;
                        otherMoveVector = -collisionExitVector;
                        otherRigidbody.gameObject.transform.Position += otherMoveVector;

                        otherCollider.calculateGlobalVertices();
                    }
                    else
                    {
                        if (otherRigidbody.IsStatic)
                        {
                            moveVector = collisionExitVector;
                            otherMoveVector = Vector3.Zero;
                            gameObject.transform.Position += moveVector;
                            colliderEndPoint += moveVector;

                            collider.calculateGlobalVertices();
                        }
                        else
                        {
                            double totalMass = mass + otherRigidbody.mass;
                            moveVector = collisionExitVector * otherRigidbody.mass / totalMass;
                            otherMoveVector = -collisionExitVector * mass / totalMass;
                            colliderEndPoint += moveVector;
                            gameObject.transform.Position += moveVector;
                            otherRigidbody.gameObject.transform.Position += otherMoveVector;

                            collider.calculateGlobalVertices();
                            otherCollider.calculateGlobalVertices();
                        }
                    }

                    Vector3 collisionPoint = Collider.GetAverageCollisionPoint(collider, otherCollider, colliderEndPoint, collisionExitNormal);
                    if (double.IsNaN(collisionPoint.x) || double.IsNaN(collisionPoint.y) || double.IsNaN(collisionPoint.z))
                    {
                        collisionPoint = Collider.GetAverageCollisionPoint(collider, otherCollider, colliderEndPoint, collisionExitNormal);
                    }

                    Vector3 newtonIteration(Func<Vector3, Vector3> f, Vector3 start)
                    {
                        Vector3 baseValue = f(start);
                        Matrix3x3 jacobi = new Matrix3x3();
                        Vector3 delta = f(start + new Vector3(Constants.Epsilon, 0.0, 0.0)) - baseValue;
                        jacobi.v00 = delta.x / Constants.Epsilon;
                        jacobi.v10 = delta.y / Constants.Epsilon;
                        jacobi.v20 = delta.z / Constants.Epsilon;
                        delta = f(start + new Vector3(0.0, Constants.Epsilon, 0.0)) - baseValue;
                        jacobi.v01 = delta.x / Constants.Epsilon;
                        jacobi.v11 = delta.y / Constants.Epsilon;
                        jacobi.v21 = delta.z / Constants.Epsilon;
                        delta = f(start + new Vector3(0.0, 0.0, Constants.Epsilon)) - baseValue;
                        jacobi.v02 = delta.x / Constants.Epsilon;
                        jacobi.v12 = delta.y / Constants.Epsilon;
                        jacobi.v22 = delta.z / Constants.Epsilon;

                        Vector3 dx = jacobi.inversed() * (-baseValue);

                        double sqrLength = baseValue.squaredLength();
                        while (dx.squaredLength() > Constants.Epsilon && f(start + dx).squaredLength() > sqrLength)
                        {
                            dx.x /= 2.0;
                            dx.y /= 2.0;
                            dx.z /= 2.0;
                        }

                        return start + dx;
                    }

                    Matrix3x3 inverseTensor = getInverseGlobalInertiaTensor();
                    Matrix3x3 otherInverseTensor = otherRigidbody.getInverseGlobalInertiaTensor();
                    Vector3 r1 = collisionPoint - gameObject.transform.Position;
                    Vector3 r2 = collisionPoint - otherRigidbody.gameObject.transform.Position;
                    Vector3 vp = Velocity + AngularVelocity % r1;
                    Vector3 othervp = otherRigidbody.Velocity + otherRigidbody.AngularVelocity % r2;
                    Vector3 dvn = vp.projectOnVector(collisionExitNormal) - othervp.projectOnVector(collisionExitNormal);
                    Vector3 dvf = vp.projectOnFlat(collisionExitNormal) - othervp.projectOnFlat(collisionExitNormal);
                    double bounciness = Material.GetComdinedBouncinessWith(otherRigidbody.Material);
                    double friction = Material.GetCombinedFrictionWith(otherRigidbody.Material);

                    Func<Vector3, Vector3> func = (F) => (F * (1.0 / mass + 1.0 / otherRigidbody.mass) +
                                                          (inverseTensor * (r1 % F)) % r1 + (otherInverseTensor * (r2 % F)) % r2 +
                                                          dvn * (1 + bounciness) + dvf * friction);
                    //if (IsStatic)
                    //{
                    //    func = (F) => (F * (1.0 / otherRigidbody.mass) + (otherInverseTensor * (r2 % F)) % r2 + vr * (1 + e));
                    //}
                    //else
                    //{
                    //    if (otherRigidbody.IsStatic)
                    //    {
                    //        func = (F) => (F * (1.0 / mass) + (inverseTensor * (r1 % F)) % r1 + vr * (1 + e));
                    //    }
                    //    else
                    //    {
                    //        func = (F) => (F * (1.0 / mass + 1.0 / otherRigidbody.mass) +
                    //                                      (inverseTensor * (r1 % F)) % r1 + (otherInverseTensor * (r2 % F)) % r2 +
                    //                                      vr * (1 + e));
                    //    }
                    //}

                    Vector3 impulse = newtonIteration(func, Vector3.Zero);
                    
                    if (!IsStatic)
                        addImpulseAtPoint(impulse, collisionPoint);
                    if (!otherRigidbody.IsStatic)
                        otherRigidbody.addImpulseAtPoint(-impulse, collisionPoint);

                    gameObject.transform.Position -= moveVector;
                    collisionExitVectors.Add(moveVector);
                    otherRigidbody.gameObject.transform.Position -= otherMoveVector;
                    otherRigidbody.collisionExitVectors.Add(otherMoveVector);
                    collider.calculateGlobalVertices();
                    otherCollider.calculateGlobalVertices();
                }

            double newSystemEnergy = mass * Velocity * Velocity / 2.0 + AngularVelocity * getInverseGlobalInertiaTensor() * AngularVelocity / 2.0 +
                otherRigidbody.mass * otherRigidbody.Velocity * otherRigidbody.Velocity / 2.0 + otherRigidbody.AngularVelocity * otherRigidbody.getInverseGlobalInertiaTensor() * otherRigidbody.AngularVelocity / 2.0;
            double systemEnergyDifference = newSystemEnergy - systemEnergy;
        }
    }
}
