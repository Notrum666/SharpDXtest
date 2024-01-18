using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using LinearAlgebra;

namespace Engine.BaseAssets.Components
{
    [Flags]
    public enum FreezeRotationFlags
    {
        None = 0,
        X = 1 << 1,
        Y = 1 << 2,
        Z = 1 << 3,
    }

    public sealed class Rigidbody : BehaviourComponent
    {
        public static Vector3 GravitationalAcceleration = new Vector3(0, 0, -9.8);

        #region ComponentData

        [SerializedField]
        private double mass = 1.0;
        [SerializedField]
        private Vector3 velocity = Vector3.Zero;
        [SerializedField]
        private Vector3 angularVelocity = Vector3.Zero;
        [SerializedField]
        private Vector3 inertiaTensor = new Vector3(1.0, 1.0, 1.0);

        [SerializedField]
        private double linearDrag = 0.05;
        [SerializedField]
        private double angularDrag = 0.05;

        [SerializedField]
        private bool isStatic = false;
        [SerializedField]
        private bool freezeMovement = false;
        [SerializedField]
        private bool freezeRotationX = false;
        [SerializedField]
        private bool freezeRotationY = false;
        [SerializedField]
        private bool freezeRotationZ = false;

        public Ranged<double> Mass => new Ranged<double>(ref mass, min: 0 + double.Epsilon, onSet: RecalculateInverseMass);
        public Vector3 Velocity { get => velocity; set => velocity = value; }
        public Vector3 AngularVelocity { get => angularVelocity; set => angularVelocity = value; }
        public Vector3 InertiaTensor
        {
            get => inertiaTensor;
            private set
            {
                inertiaTensor = value;
                RecalculateInverseGlobalInertiaTensor();
            }
        }

        public Ranged<double> LinearDrag => new Ranged<double>(ref linearDrag, min: 0);
        public Ranged<double> AngularDrag => new Ranged<double>(ref angularDrag, min: 0);

        public bool IsStatic
        {
            get => isStatic;
            set
            {
                isStatic = value;
                RecalculateInverseMass();
                RecalculateInverseGlobalInertiaTensor();
            }
        }
        public bool FreezeMovement
        {
            get => freezeMovement;
            set
            {
                freezeMovement = value;
                RecalculateInverseMass();
            }
        }

        public FreezeRotationFlags GetFreezeRotationFlags()
        {
            return (freezeRotationX ? FreezeRotationFlags.X : FreezeRotationFlags.None)
                   | (freezeRotationY ? FreezeRotationFlags.Y : FreezeRotationFlags.None)
                   | (freezeRotationZ ? FreezeRotationFlags.Z : FreezeRotationFlags.None);
        }

        public void SetFreezeRotationFlags(FreezeRotationFlags freezeRotationFlags)
        {
            freezeRotationX = freezeRotationFlags.HasFlag(FreezeRotationFlags.X);
            freezeRotationY = freezeRotationFlags.HasFlag(FreezeRotationFlags.Y);
            freezeRotationZ = freezeRotationFlags.HasFlag(FreezeRotationFlags.Z);
            RecalculateInverseGlobalInertiaTensor();
        }

        public override void OnFieldChanged(FieldInfo fieldInfo)
        {
            base.OnFieldChanged(fieldInfo);

            switch (fieldInfo.Name)
            {
                case nameof(isStatic):
                    IsStatic = isStatic;
                    return;
                case nameof(mass):
                    Mass.Set(mass);
                    return;
                case nameof(inertiaTensor):
                    InertiaTensor = inertiaTensor;
                    return;
                case nameof(freezeRotationX):
                case nameof(freezeRotationY):
                case nameof(freezeRotationZ):
                    RecalculateInverseGlobalInertiaTensor();
                    return;
            }
        }
        
        internal override void OnDeserialized()
        {
            RecalculateInverseMass();
            RecalculateInverseGlobalInertiaTensor();
        }

        #endregion ComponentData

        public PhysicalMaterial Material { get; set; } = new PhysicalMaterial();


        private double inverseMass = 1.0;
        private Matrix3x3 inverseGlobalInertiaTensor = Matrix3x3.Identity;

        private Vector3 velocityChange = new Vector3();
        private Vector3 angularVelocityChange = new Vector3();
        private readonly List<Vector3> collisionExitVectors = new List<Vector3>();

        //private const int LinearVelocitySleepCounterBase = 5;
        //private const int AngularVelocitySleepCounterBase = 5;
        //private int linearVelocitySleepCounter = 0;
        //private int angularVelocitySleepCounter = 0;
        //private double linearSleepThresholdSquared = 0.005;
        //private double angularSleepThresholdSquared = 0.005;

        public override void FixedUpdate()
        {
            AddForce(GravitationalAcceleration * mass);
            RecalculateInertiaTensor();

            Transform t = GameObject.Transform;

            Velocity *= 1.0 - Time.DeltaTime * linearDrag;
            AngularVelocity *= 1.0 - Time.DeltaTime * angularDrag;

            //if (Velocity.squaredLength() <= linearSleepThresholdSquared)
            //{
            //    if (linearVelocitySleepCounter > 0)
            //        linearVelocitySleepCounter--;
            //    else
            //        Velocity = Vector3.Zero;
            //}
            //else
            //    linearVelocitySleepCounter = LinearVelocitySleepCounterBase;

            //if (AngularVelocity.squaredLength() <= angularSleepThresholdSquared)
            //{
            //    if (angularVelocitySleepCounter > 0)
            //        angularVelocitySleepCounter--;
            //    else
            //        AngularVelocity = Vector3.Zero;
            //}
            //else
            //    angularVelocitySleepCounter = AngularVelocitySleepCounterBase;

            if (t.Parent == null)
            {
                t.LocalPosition += Velocity * Time.DeltaTime;

                t.LocalRotation = (t.LocalRotation +
                                   0.5 * new Quaternion(0.0, AngularVelocity.x, AngularVelocity.y, AngularVelocity.z) * t.LocalRotation * Time.DeltaTime).normalized();
            }
            else
            {
                t.LocalPosition += t.Parent.Rotation.inverse() * Velocity * Time.DeltaTime;

                // faster
                Vector3 localAngularVelocity = t.Parent.Rotation.inverse() * AngularVelocity;
                t.LocalRotation = (t.LocalRotation +
                                   0.5 * new Quaternion(0.0, localAngularVelocity.x, localAngularVelocity.y, localAngularVelocity.z) *
                                   t.LocalRotation * Time.DeltaTime).normalized();
                // simpler
                //t.Rotation = (t.Rotation +
                //    0.5 * new Quaternion(0.0, angularVelocity.x, angularVelocity.y, angularVelocity.z) *
                //    t.Rotation * Time.DeltaTime).normalized();
            }
        }

        private void RecalculateInertiaTensor()
        {
            List<Collider> colliders = GameObject.GetComponents<Collider>().Where(coll => coll.Enabled).ToList();
            if (!colliders.Any())
            {
                inertiaTensor = new Vector3(1.0, 1.0, 1.0);
                return;
            }

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

        private void RecalculateInverseMass()
        {
            inverseMass = IsStatic || FreezeMovement ? 0.0 : 1.0 / mass;
        }

        private void RecalculateInverseGlobalInertiaTensor()
        {
            if (isStatic)
            {
                inverseGlobalInertiaTensor = new Matrix3x3();
                return;
            }
            Matrix3x3 model = Matrix3x3.FromQuaternion(GameObject.Transform.Rotation);
            inverseGlobalInertiaTensor = model * new Matrix3x3(freezeRotationX ? 0.0 : 1.0 / InertiaTensor.x, 0.0, 0.0,
                                                               0.0, freezeRotationY ? 0.0 : 1.0 / InertiaTensor.y, 0.0,
                                                               0.0, 0.0, freezeRotationZ ? 0.0 : 1.0 / InertiaTensor.z) * model.transposed();
        }

        public void AddForce(Vector3 force)
        {
            velocityChange += force * Time.DeltaTime * inverseMass;
            //linearVelocitySleepCounter = LinearVelocitySleepCounterBase;
        }

        public void AddImpulse(Vector3 impulse)
        {
            velocityChange += impulse * inverseMass;
            //linearVelocitySleepCounter = LinearVelocitySleepCounterBase;
        }

        public void AddTorque(Vector3 torque)
        {
            angularVelocityChange += torque * inverseGlobalInertiaTensor * Time.DeltaTime;
            //angularVelocitySleepCounter = AngularVelocitySleepCounterBase;
        }

        public void AddAngularImpulse(Vector3 angularImpulse)
        {
            angularVelocityChange += angularImpulse * inverseGlobalInertiaTensor;
            //angularVelocitySleepCounter = AngularVelocitySleepCounterBase;
        }

        public void AddForceAtPoint(Vector3 force, Vector3 point)
        {
            velocityChange += force * Time.DeltaTime * inverseMass;
            angularVelocityChange += (point - GameObject.Transform.Position) % force * inverseGlobalInertiaTensor * Time.DeltaTime;

            //linearVelocitySleepCounter = LinearVelocitySleepCounterBase;
            //angularVelocitySleepCounter = AngularVelocitySleepCounterBase;
        }

        public void AddImpulseAtPoint(Vector3 impulse, Vector3 point)
        {
            velocityChange += impulse * inverseMass;
            angularVelocityChange += (point - GameObject.Transform.Position) % impulse * inverseGlobalInertiaTensor;

            //linearVelocitySleepCounter = LinearVelocitySleepCounterBase;
            //angularVelocitySleepCounter = AngularVelocitySleepCounterBase;
        }

        public void ApplyChanges()
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

            GameObject.Transform.Position += result;
            collisionExitVectors.Clear();
        }

        #region Collision

        public delegate void CollisionEvent(Rigidbody sender, Collider col, Collider other);
        public event CollisionEvent OnCollisionBegin;
        public event CollisionEvent OnCollision;
        public event CollisionEvent OnCollisionEnd;

        private List<KeyValuePair<Collider, Collider>> prevCollidingPairs = new List<KeyValuePair<Collider, Collider>>(); //TODO: Change to tuples
        private List<KeyValuePair<Collider, Collider>> collidingPairs = new List<KeyValuePair<Collider, Collider>>(); //TODO: Change to tuples

        public void SolveCollisionWith(Rigidbody otherRigidbody)
        {
            if (!Enabled || !otherRigidbody.Enabled || IsStatic && otherRigidbody.IsStatic)
                return;

            IEnumerable<Collider> colliders = GameObject.GetComponents<Collider>().Where(coll => coll.Enabled);
            List<Collider> otherColliders = otherRigidbody.GameObject.GetComponents<Collider>().Where(coll => coll.Enabled).ToList();

            foreach (Collider collider in colliders)
            {
                foreach (Collider otherCollider in otherColliders)
                {
                    Vector3? _collisionExitVector; //TODO: whut?
                    Vector3? _collisionExitNormal;
                    Vector3? _colliderEndPoint;
                    if (!collider.GetCollisionExitVector(otherCollider, out _collisionExitVector, out _collisionExitNormal, out _colliderEndPoint))
                        continue;

                    Vector3 collisionExitVector = (Vector3)_collisionExitVector;
                    Vector3 collisionExitNormal = (Vector3)_collisionExitNormal;
                    Vector3 colliderEndPoint = (Vector3)_colliderEndPoint;
                    collisionExitNormal.normalize();

                    collidingPairs.Add(new KeyValuePair<Collider, Collider>(collider, otherCollider));
                    otherRigidbody.collidingPairs.Add(new KeyValuePair<Collider, Collider>(otherCollider, collider));

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
                        GameObject.Transform.Position += moveVector;
                        collider.UpdateData();
                    }
                    if (!otherMoveVector.isZero())
                    {
                        otherRigidbody.GameObject.Transform.Position += otherMoveVector;
                        otherCollider.UpdateData();
                    }

                    Vector3 collisionPoint = Collider.GetAverageCollisionPoint(collider, otherCollider, colliderEndPoint, collisionExitNormal);
                    if (double.IsNaN(collisionPoint.x) || double.IsNaN(collisionPoint.y) || double.IsNaN(collisionPoint.z))
                    {
                        if (!moveVector.isZero())
                        {
                            GameObject.Transform.Position -= moveVector;
                            collisionExitVectors.Add(moveVector);
                            collider.UpdateData();
                        }
                        if (!otherMoveVector.isZero())
                        {
                            otherRigidbody.GameObject.Transform.Position -= otherMoveVector;
                            otherRigidbody.collisionExitVectors.Add(otherMoveVector);
                            otherCollider.UpdateData();
                        }
                        return;
                    }

                    Vector3 r1 = collisionPoint - GameObject.Transform.Position;
                    Vector3 r2 = collisionPoint - otherRigidbody.GameObject.Transform.Position;
                    Vector3 vp = Velocity + AngularVelocity % r1;
                    Vector3 othervp = otherRigidbody.Velocity + otherRigidbody.AngularVelocity % r2;
                    Vector3 dvn = vp.projectOnVector(collisionExitNormal) - othervp.projectOnVector(collisionExitNormal);
                    Vector3 dvf = vp.projectOnFlat(collisionExitNormal) - othervp.projectOnFlat(collisionExitNormal);
                    double bounciness = Material.GetComdinedBouncinessWith(otherRigidbody.Material);
                    double friction = Material.GetCombinedFrictionWith(otherRigidbody.Material);

                    Func<Vector3, Vector3> func = (F) => F * (inverseMass + otherRigidbody.inverseMass) +
                                                         inverseGlobalInertiaTensor * (r1 % F) % r1 +
                                                         otherRigidbody.inverseGlobalInertiaTensor * (r2 % F) % r2 +
                                                         dvn * (1 + bounciness) + dvf * friction;

                    Vector3 impulse = NewtonIteration(func, Vector3.Zero);

                    AddImpulseAtPoint(impulse, collisionPoint);

                    bounciness = otherRigidbody.Material.GetComdinedBouncinessWith(Material);
                    friction = otherRigidbody.Material.GetCombinedFrictionWith(Material);

                    func = (F) => F * (inverseMass + otherRigidbody.inverseMass) +
                                  inverseGlobalInertiaTensor * (r1 % F) % r1 +
                                  otherRigidbody.inverseGlobalInertiaTensor * (r2 % F) % r2 +
                                  dvn * (1 + bounciness) + dvf * friction;

                    impulse = NewtonIteration(func, Vector3.Zero);

                    otherRigidbody.AddImpulseAtPoint(-impulse, collisionPoint);

                    if (!moveVector.isZero())
                    {
                        GameObject.Transform.Position -= moveVector;
                        collisionExitVectors.Add(moveVector);
                        collider.UpdateData();
                    }
                    if (!otherMoveVector.isZero())
                    {
                        otherRigidbody.GameObject.Transform.Position -= otherMoveVector;
                        otherRigidbody.collisionExitVectors.Add(otherMoveVector);
                        otherCollider.UpdateData();
                    }
                }
            }
        }

        internal void UpdateCollidingPairs()
        {
            foreach (KeyValuePair<Collider, Collider> pair in prevCollidingPairs)
            {
                if (!collidingPairs.Contains(pair))
                    OnCollisionEnd?.Invoke(this, pair.Key, pair.Value);
            }
            foreach (KeyValuePair<Collider, Collider> pair in collidingPairs)
            {
                if (!prevCollidingPairs.Contains(pair))
                    OnCollisionBegin?.Invoke(this, pair.Key, pair.Value);
                OnCollision?.Invoke(this, pair.Key, pair.Value);
            }

            prevCollidingPairs = new List<KeyValuePair<Collider, Collider>>(collidingPairs);
            collidingPairs.Clear();
        }

        private Vector3 NewtonIteration(Func<Vector3, Vector3> f, Vector3 start)
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

            return start + jacobi.inverse() * -baseValue;
        }

        #endregion Collision

    }
}