using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Engine;
using Engine.BaseAssets.Components;
using LinearAlgebra;
using SharpDX.DirectInput;

namespace SharpDXtest.Assets.Components
{
    class GravityForce : Component
    {
        public double periodInDays;
        public override void fixedUpdate()
        {
            Rigidbody rb = gameObject.getComponent<Rigidbody>();
            if (rb != null)
                rb.addForce(new Vector3(0, 0, -9.8 * rb.Mass));
        }
    }
}
