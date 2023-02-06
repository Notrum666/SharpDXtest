using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Engine.BaseAssets.Components;

namespace SharpDXtest.Assets.Components
{
    class CubeScript : Component
    {
        public override void update()
        {

        }
        public override void fixedUpdate()
        {
            Rigidbody rb = gameObject.getComponent<Rigidbody>();
            rb.addForce(new LinearAlgebra.Vector3(0.0, 0.0, -9.8 * rb.Mass));
        }
    }
}
