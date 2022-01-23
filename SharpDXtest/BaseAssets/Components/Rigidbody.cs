using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpDXtest.BaseAssets.Components
{
    [Flags]
    enum FreezeRotationFlags
    {
        None = 0,
        X = 1,
        Y = 2,
        Z = 4
    }
    sealed class Rigidbody : Component
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
                recalculateInverseMass();
            } 
        }
        private Vector3 inertiaTensor = new Vector3(1.0, 1.0, 1.0);
        public Vector3 InertiaTensor
        {
            get
            {
                return inertiaTensor;
            }
            private set
            {
                inertiaTensor = value;
                recalculateInverseGlobalInertiaTensor();
            }
        }

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

        private bool isStatic = false;
        public bool IsStatic
        {
            get
            {
                return isStatic;
            }
            set
            {
                isStatic = value;
                recalculateInverseMass();
                recalculateInverseGlobalInertiaTensor();
            }
        }
        private bool freezeMovement = false;
        public bool FreezeMovement
        {
            get
            {
                return freezeMovement;
            }
            set
            {
                freezeMovement = value;
                recalculateInverseMass();
            }
        }
        private FreezeRotationFlags freezeRotation = FreezeRotationFlags.None;
        public FreezeRotationFlags FreezeRotation
        {
            get
            {
                return freezeRotation;
            }
            set
            {
                freezeRotation = value;
                recalculateInverseGlobalInertiaTensor();
            }
        }

        private double inverseMass = 1.0;
        private Matrix3x3 inverseGlobalInertiaTensor = Matrix3x3.Identity;

        public PhysicalMaterial Material { get; set; }

        private Vector3 velocityChange = new Vector3();
        private Vector3 angularVelocityChange = new Vector3();
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
                t.localPosition += t.Parent.Rotation.inverse() * Velocity * Time.DeltaTime;

                // faster
                Vector3 localAngularVelocity = t.Parent.Rotation.inverse() * AngularVelocity;
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
            if (colliders.Length == 0)
                inertiaTensor = new Vector3(1.0, 1.0, 1.0);
            double massSum = 0.0;
            foreach (Collider collider in colliders)
                massSum += collider.MassPart;
            if (massSum == 0.0)
                throw new Exception("At least one collider must have positive mass part");

            Vector3 result = new Vector3();
            foreach (Collider collider in colliders)
                result += collider.InertiaTensor + collider.Offset.compMul(collider.Offset) * collider.MassPart / massSum;

            InertiaTensor = result;
        }
        private void recalculateInverseMass()
        {
            inverseMass = (IsStatic || FreezeMovement ? 0.0 : 1.0 / mass);
        }
        private void recalculateInverseGlobalInertiaTensor()
        {
            if (isStatic)
            {
                inverseGlobalInertiaTensor = new Matrix3x3();
                return;
            }
            Matrix3x3 model = Matrix3x3.FromQuaternion(gameObject.transform.Rotation);
            inverseGlobalInertiaTensor = model * new Matrix3x3(FreezeRotation.HasFlag(FreezeRotationFlags.X) ? 0.0 : 1.0 / InertiaTensor.x, 0.0, 0.0,
                                                         0.0, FreezeRotation.HasFlag(FreezeRotationFlags.Y) ? 0.0 : 1.0 / InertiaTensor.y, 0.0,
                                                         0.0, 0.0, FreezeRotation.HasFlag(FreezeRotationFlags.Z) ? 0.0 : 1.0 / InertiaTensor.z) * model.transposed();
        }
        public void addForce(Vector3 force)
        {
            velocityChange += force * Time.DeltaTime * inverseMass;
        }
        public void addImpulse(Vector3 impulse)
        {
            velocityChange += impulse * inverseMass;
        }
        public void addTorque(Vector3 torque)
        {
            angularVelocityChange += torque * inverseGlobalInertiaTensor * Time.DeltaTime;
        }
        public void addAngularImpulse(Vector3 angularImpulse)
        {
            angularVelocityChange += angularImpulse * inverseGlobalInertiaTensor;
        }
        public void addForceAtPoint(Vector3 force, Vector3 point)
        {
            velocityChange += force * Time.DeltaTime * inverseMass;

            angularVelocityChange += (point - gameObject.transform.Position) % force * inverseGlobalInertiaTensor * Time.DeltaTime;
        }
        public void addImpulseAtPoint(Vector3 impulse, Vector3 point)
        {
            velocityChange += impulse * inverseMass;

            angularVelocityChange += (point - gameObject.transform.Position) % impulse * inverseGlobalInertiaTensor;
        }
        public void applyChanges()
        {
            Velocity += velocityChange;
            AngularVelocity += angularVelocityChange;
            velocityChange = new Vector3();
            angularVelocityChange = new Vector3();

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

                    Vector3 moveVector, otherMoveVector;
                    if (IsStatic)
                    {
                        moveVector = Vector3.Zero;
                        otherMoveVector = -collisionExitVector;
                    }
                    else
                    {
                        if (otherRigidbody.IsStatic)
                        {
                            moveVector = collisionExitVector;
                            otherMoveVector = Vector3.Zero;
                            colliderEndPoint += moveVector;
                        }
                        else
                        {
                            double totalMass = mass + otherRigidbody.mass;
                            moveVector = collisionExitVector * otherRigidbody.mass / totalMass;
                            otherMoveVector = -collisionExitVector * mass / totalMass;
                            colliderEndPoint += moveVector;
                        }
                    }

                    if (!moveVector.isZero())
                    {
                        gameObject.transform.Position += moveVector;
                        collider.calculateGlobalVertices();
                    }
                    if (!otherMoveVector.isZero())
                    {
                        otherRigidbody.gameObject.transform.Position += otherMoveVector;
                        otherCollider.calculateGlobalVertices();
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

                        return start + jacobi.inversed() * (-baseValue);
                    }

                    Vector3 r1 = collisionPoint - gameObject.transform.Position;
                    Vector3 r2 = collisionPoint - otherRigidbody.gameObject.transform.Position;
                    Vector3 vp = Velocity + AngularVelocity % r1;
                    Vector3 othervp = otherRigidbody.Velocity + otherRigidbody.AngularVelocity % r2;
                    Vector3 dvn = vp.projectOnVector(collisionExitNormal) - othervp.projectOnVector(collisionExitNormal);
                    Vector3 dvf = vp.projectOnFlat(collisionExitNormal) - othervp.projectOnFlat(collisionExitNormal);
                    double bounciness = Material.GetComdinedBouncinessWith(otherRigidbody.Material);
                    double friction = Material.GetCombinedFrictionWith(otherRigidbody.Material);

                    Func<Vector3, Vector3> func = (F) => (F * (inverseMass + otherRigidbody.inverseMass) +
                                                          (inverseGlobalInertiaTensor * (r1 % F)) % r1 +
                                                          (otherRigidbody.inverseGlobalInertiaTensor * (r2 % F)) % r2 +
                                                          dvn * (1 + bounciness) + dvf * friction);

                    Vector3 impulse = newtonIteration(func, Vector3.Zero);

                    addImpulseAtPoint(impulse, collisionPoint);

                    bounciness = otherRigidbody.Material.GetComdinedBouncinessWith(Material);
                    friction = otherRigidbody.Material.GetCombinedFrictionWith(Material);

                    func = (F) => (F * (inverseMass + otherRigidbody.inverseMass) +
                                                          (inverseGlobalInertiaTensor * (r1 % F)) % r1 +
                                                          (otherRigidbody.inverseGlobalInertiaTensor * (r2 % F)) % r2 +
                                                          dvn * (1 + bounciness) + dvf * friction);

                    impulse = newtonIteration(func, Vector3.Zero);

                    otherRigidbody.addImpulseAtPoint(-impulse, collisionPoint);

                    if (!moveVector.isZero())
                    {
                        gameObject.transform.Position -= moveVector;
                        collisionExitVectors.Add(moveVector);
                        collider.calculateGlobalVertices();
                    }
                    if (!otherMoveVector.isZero())
                    {
                        otherRigidbody.gameObject.transform.Position -= otherMoveVector;
                        otherRigidbody.collisionExitVectors.Add(otherMoveVector);
                        otherCollider.calculateGlobalVertices();
                    }
                }
        }
    }
}
