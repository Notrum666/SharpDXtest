using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Engine;
using Engine.BaseAssets.Components;
using LinearAlgebra;
using SharpDX.DirectInput;

namespace SharpDXtest.Assets.Components
{
    class BallRestarter : Component
    {
        private Random rng = new Random();
        private double speed = 15;
        private bool initialized = false;
        public GameObject leftRacket, rightRacket;
        public int scoreLeft = 0, scoreRight = 0;
        public event Action<int, int> OnScoreChanged;

        public override void Update()
        {
            Rigidbody rb = GameObject.GetComponent<Rigidbody>();
            if (!initialized)
            {
                initialized = true;
                rb.OnCollisionBegin += onCollisionBegin;
            }
            if (InputManager.IsKeyPressed(Key.Space) && rb.Velocity.isZero())
            {
                double angle = 60.0 + rng.NextDouble() * 60.0;
                if (rng.Next(2) > 0)
                    angle += 180.0;
                angle = angle / 180.0 * Math.PI;
                rb.Velocity = new Vector3(0, Math.Sin(angle), Math.Cos(angle));
            }

            if (!rb.Velocity.isZero())
                rb.Velocity = rb.Velocity.normalized() * speed;

            if (GameObject.Transform.Position.y > 21 || GameObject.Transform.Position.y < -21)
            {
                if (GameObject.Transform.Position.y > 21)
                    scoreLeft++;
                else
                    scoreRight++;
                OnScoreChanged?.Invoke(scoreLeft, scoreRight);
                rb.Velocity = Vector3.Zero;
                GameObject.Transform.Position = Vector3.Zero;
            }
        }

        private void onCollisionBegin(Rigidbody sender, Collider col, Collider other)
        {
            if (other.GameObject == leftRacket || other.GameObject == rightRacket)
            {
                double deltaZ = GameObject.Transform.Position.z - other.GameObject.Transform.Position.z;
                sender.Velocity += new Vector3(0, 0, deltaZ * 4);
                speed += 1;
            }
        }
    }
}