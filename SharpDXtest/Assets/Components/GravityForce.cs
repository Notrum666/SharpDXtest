using Engine.BaseAssets.Components;

using LinearAlgebra;

namespace SharpDXtest.Assets.Components
{
    class GravityForce : BehaviourComponent
    {
        public double periodInDays;

        public override void FixedUpdate()
        {
            Rigidbody rb = GameObject.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce(new Vector3(0, 0, -9.8 * rb.Mass));
        }
    }
}