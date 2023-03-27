using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.DirectInput;

using LinearAlgebra;
using Engine;
using Engine.BaseAssets.Components;

namespace SharpDXtest.Assets.Components
{
    public class CameraController : Component
    {
        public float speed;
        public override void update()
        {
            float curSpeed = speed * (float)Time.DeltaTime;

            if (InputManager.IsKeyPressed(Key.E))
            {
                Time.TimeScale *= 2;
                speed /= 2;
            }
            if (InputManager.IsKeyPressed(Key.Q))
            {
                Time.TimeScale /= 2;
                speed *= 2;
            }

            if (InputManager.IsKeyDown(Key.LeftShift))
                curSpeed *= 5f;
            if (InputManager.IsKeyDown(Key.A))
                gameObject.transform.LocalPosition -= gameObject.transform.LocalRight * curSpeed;
            if (InputManager.IsKeyDown(Key.D))
                gameObject.transform.LocalPosition += gameObject.transform.LocalRight * curSpeed;
            if (InputManager.IsKeyDown(Key.S))
                gameObject.transform.LocalPosition -= gameObject.transform.LocalForward * curSpeed;
            if (InputManager.IsKeyDown(Key.W))
                gameObject.transform.LocalPosition += gameObject.transform.LocalForward * curSpeed;
            if (InputManager.IsKeyDown(Key.C))
                // faster
                gameObject.transform.LocalPosition -= (gameObject.transform.Parent == null ? Vector3.Up : (gameObject.transform.Parent.View * Vector4.UnitZ).xyz) * curSpeed;
                // simpler
                //gameObject.transform.Position -= Vector3.Up * curSpeed;
            if (InputManager.IsKeyDown(Key.Space))
                // faster
                gameObject.transform.LocalPosition += (gameObject.transform.Parent == null ? Vector3.Up : (gameObject.transform.Parent.View * Vector4.UnitZ).xyz) * curSpeed;
                // simpler
                //gameObject.transform.Position += Vector3.Up * curSpeed;
            
            Vector2 mouseDelta = InputManager.GetMouseDelta() / 1000;
            
            if (!mouseDelta.isZero())
            {
                // faster
                gameObject.transform.LocalRotation = Quaternion.FromAxisAngle(gameObject.transform.Parent == null ? Vector3.Up :
                                                            (gameObject.transform.Parent.View * Vector4.UnitZ).xyz, -mouseDelta.x) *
                                                     Quaternion.FromAxisAngle(gameObject.transform.LocalRight, -mouseDelta.y) *
                                                     gameObject.transform.LocalRotation;
                // simpler
                //gameObject.transform.Rotation = Quaternion.FromAxisAngle(Vector3.Up, -mouseDelta.x) *
                //                                     Quaternion.FromAxisAngle(gameObject.transform.right, -mouseDelta.y) *
                //                                     gameObject.transform.Rotation;
            }
        }
    }
}