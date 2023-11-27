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
    class OrbitalRotation : Component
    {
        public double periodInDays;
        public double radius;
        private double curAngle = 0;

        public override void Update()
        {
            double angularSpeed = 90 * 365.0 / periodInDays * Time.DeltaTime;
            curAngle += angularSpeed;
            GameObject.Transform.LocalPosition = new Vector3(Math.Sin(curAngle / 180.0 * Math.PI), Math.Cos(curAngle / 180.0 * Math.PI), 0.0) * radius;
        }
    }
}