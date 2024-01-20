using System;

using Engine;
using Engine.BaseAssets.Components;

using System.Windows.Input;

using LinearAlgebra;

namespace SharpDXtest.Assets.Components
{
    public class EditorCameraController : BehaviourComponent
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

            UpdateInput();
        }

        private void UpdateInput()
        {
            if (Input.IsMouseButtonReleased(MouseButton.Right))
            {
                Input.CursorState = CursorState.Default;
                return;
            }

            if (!Input.IsMouseButtonDown(MouseButton.Right))
                return;

            if (Input.IsMouseButtonPressed(MouseButton.Right))
                Input.CursorState = CursorState.HiddenAndLocked;

            if (Input.IsKeyDown(Key.A))
                targetVelocity -= GameObject.Transform.Right;
            if (Input.IsKeyDown(Key.D))
                targetVelocity += GameObject.Transform.Right;
            if (Input.IsKeyDown(Key.S))
                targetVelocity -= GameObject.Transform.Forward;
            if (Input.IsKeyDown(Key.W))
                targetVelocity += GameObject.Transform.Forward;
            if (Input.IsKeyDown(Key.C))
                targetVelocity -= GameObject.Transform.Up;
            if (Input.IsKeyDown(Key.Space))
                targetVelocity += GameObject.Transform.Up;

            curMaxSpeed = maxSpeed;
            if (Input.IsKeyDown(Key.LeftShift))
                curMaxSpeed *= 5;
            if (Input.IsKeyDown(Key.LeftCtrl))
                curMaxSpeed /= 5;

            if (!targetVelocity.isZero())
                targetVelocity = targetVelocity.normalized() * curMaxSpeed;

            Vector2 mouseDelta = Input.GetMouseDelta() / 750;

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