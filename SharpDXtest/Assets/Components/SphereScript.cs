using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine;
using Engine.BaseAssets.Components;
using LinearAlgebra;
using SharpDX.DirectInput;

namespace SharpDXtest.Assets.Components
{
    class SphereScript : Component
    {
        private bool started = false;
        public override void update()
        {
        }
        public override void fixedUpdate()
        {
            Rigidbody rb = gameObject.getComponent<Rigidbody>();
            if (started)
                rb.addForce(new LinearAlgebra.Vector3(0.0, 0.0, -9.8 * rb.Mass));
            if (InputManager.IsKeyPressed(Key.F))
            {
                rb.Velocity = new Vector3(0.0, 20.0, 0.0);
                started = true;
            }
        }
    }
}
