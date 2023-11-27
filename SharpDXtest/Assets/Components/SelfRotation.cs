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
    class SelfRotation : Component
    {
        public double periodInDays;

        public override void Update()
        {
            double angularSpeed = 90 * 365.0 / periodInDays * Time.DeltaTime / 180.0 * Math.PI;
            GameObject.Transform.LocalRotation = GameObject.Transform.LocalRotation * Quaternion.FromAxisAngle(Vector3.Up, -angularSpeed);
        }
    }
}