using Engine.BaseAssets.Components;

using LinearAlgebra;

namespace SharpDXtest.Assets.Components
{
    class GravityForce : Component
    {
        public double periodInDays;

        public override void FixedUpdate()
        {
            Rigidbody rb = GameObject.GetComponent<Rigidbody>();
            if (rb != null)
                rb.addForce(new Vector3(0, 0, -9.8 * rb.Mass));
        }
    }
}