using System;

using Engine;
using Engine.BaseAssets.Components;

using LinearAlgebra;

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