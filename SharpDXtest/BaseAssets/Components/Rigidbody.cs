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
            } 
        }
        private Vector3 InertiaTensor { get; set; } = new Vector3(1.0, 1.0, 1.0);
        public Vector3 velocity = Vector3.Zero;
        public Vector3 angularVelocity = Vector3.Zero;

        public override void update()
        {
            Transform t = gameObject.transform;
            if (t.Parent == null)
            {
                t.localPosition += velocity * Time.DeltaTime;

                t.localRotation = (t.localRotation +
                    0.5 * new Quaternion(0.0, angularVelocity.x, angularVelocity.y, angularVelocity.z) * t.localRotation * Time.DeltaTime).normalized();
            }
            else
            {
                t.localPosition += (t.Parent.view * new Vector4(velocity * Time.DeltaTime, 0.0)).xyz;

                // faster
                Vector3 localAngularVelocity = (t.Parent.view * new Vector4(angularVelocity, 0.0)).xyz;
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
            velocity += force * Time.FixedDeltaTime / mass;
        }
        public void addImpulse(Vector3 impulse)
        {
            velocity += impulse / mass;
        }
        public void addForceAtPoint(Vector3 force, Vector3 point)
        {
            Transform t = gameObject.transform;
            Vector3 realPosition = t.Position;
            Vector3 radiusVector = point - realPosition;
            
            velocity += force.projectOnVector(radiusVector) * Time.FixedDeltaTime / mass;

            angularVelocity += (radiusVector % force).compDiv((t.model * new Vector4(InertiaTensor, 0.0)).xyz) * Time.FixedDeltaTime;
        }
        public void addImpulseAtPoint(Vector3 impulse, Vector3 point)
        {
            Transform t = gameObject.transform;
            Vector3 realPosition = t.Position;
            Vector3 radiusVector = point - realPosition;

            velocity += impulse.projectOnVector(radiusVector) / mass;

            angularVelocity += (radiusVector % impulse).compDiv((t.model * new Vector4(InertiaTensor, 0.0)).xyz);
        }
    }
}
