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
        private Vector3 InertiaTensor { get; set; } = new Vector3(1.0, 1.0, 1.0);
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

        public override void update()
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

        public void addForce(Vector3 force)
        {
            Velocity += force * Time.FixedDeltaTime / mass;
        }
        public void addImpulse(Vector3 impulse)
        {
            Velocity += impulse / mass;
        }
        public void addForceAtPoint(Vector3 force, Vector3 point)
        {
            Transform t = gameObject.transform;
            Vector3 realPosition = t.Position;
            Vector3 radiusVector = point - realPosition;
            
            Velocity += force.projectOnVector(radiusVector) * Time.FixedDeltaTime / mass;

            AngularVelocity += (radiusVector % force).compDiv((t.model * new Vector4(InertiaTensor, 0.0)).xyz) * Time.FixedDeltaTime;
        }
        public void addImpulseAtPoint(Vector3 impulse, Vector3 point)
        {
            Transform t = gameObject.transform;
            Vector3 realPosition = t.Position;
            Vector3 radiusVector = point - realPosition;

            Velocity += impulse.projectOnVector(radiusVector) / mass;

            AngularVelocity += (radiusVector % impulse).compDiv((t.model * new Vector4(InertiaTensor, 0.0)).xyz);
        }
    }
}
