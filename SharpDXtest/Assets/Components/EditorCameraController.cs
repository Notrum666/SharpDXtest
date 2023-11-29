using System;

using Engine;
using Engine.BaseAssets.Components;

using LinearAlgebra;

using SharpDX.DirectInput;

namespace SharpDXtest.Assets.Components
{
    public class EditorCameraController : Component
    {
        private double accelerationCoef = 7;
        private double maxSpeed = 20;
        private double curMaxSpeed;
        private Vector3 curVelocity = Vector3.Zero;
        private Vector3 targetVelocity = Vector3.Zero;

        public override void Update()
        {
            Vector3 velocityChange = targetVelocity - curVelocity;
            if (!velocityChange.isZero())
            {
                double max = velocityChange.length();
                velocityChange = velocityChange.normalized() * Math.Min(accelerationCoef * curMaxSpeed * Time.DeltaTime, max);

                curVelocity += velocityChange;
            }

            if (!curVelocity.isZero())
            {
                if (curVelocity.length() > curMaxSpeed)
                    curVelocity = curVelocity.normalized() * curMaxSpeed;
                GameObject.Transform.Position += curVelocity * Time.DeltaTime;
            }

            targetVelocity = Vector3.Zero;
        }
        public void UpdateInput()
        {
            if (!InputManager.IsMouseButtonDown(1))
                return;

            if (InputManager.IsKeyDown(Key.A))
                targetVelocity -= GameObject.Transform.Right;
            if (InputManager.IsKeyDown(Key.D))
                targetVelocity += GameObject.Transform.Right;
            if (InputManager.IsKeyDown(Key.S))
                targetVelocity -= GameObject.Transform.Forward;
            if (InputManager.IsKeyDown(Key.W))
                targetVelocity += GameObject.Transform.Forward;
            if (InputManager.IsKeyDown(Key.C))
                targetVelocity -= GameObject.Transform.Up;
            if (InputManager.IsKeyDown(Key.Space))
                targetVelocity += GameObject.Transform.Up;

            curMaxSpeed = maxSpeed;
            if (InputManager.IsKeyDown(Key.LeftShift))
                curMaxSpeed *= 5;
            if (InputManager.IsKeyDown(Key.LeftControl))
                curMaxSpeed /= 5;

            if (!targetVelocity.isZero())
                targetVelocity = targetVelocity.normalized() * curMaxSpeed;

            Vector2 mouseDelta = InputManager.GetMouseDelta() / 750;

            if (!mouseDelta.isZero())
            // faster
            {
                GameObject.Transform.LocalRotation = Quaternion.FromAxisAngle(GameObject.Transform.Parent == null ? Vector3.Up :
                                                                                  GameObject.Transform.Parent.View.TransformDirection(Vector3.UnitZ), -mouseDelta.x) *
                                                     Quaternion.FromAxisAngle(GameObject.Transform.LocalRight, -mouseDelta.y) *
                                                     GameObject.Transform.LocalRotation;
            }
            // simpler
            //gameObject.transform.Rotation = Quaternion.FromAxisAngle(Vector3.Up, -mouseDelta.x) *
            //                                     Quaternion.FromAxisAngle(gameObject.transform.right, -mouseDelta.y) *
            //                                     gameObject.transform.Rotation;
        }
    }
}