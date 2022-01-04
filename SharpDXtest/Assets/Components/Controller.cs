using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.DirectInput;
using SharpDXtest;
using SharpDXtest.BaseAssets.Components;

namespace SharpDXtest.Assets.Components
{
    public class Controller : Component
    {
        public float speed = 3f;
        public override void update()
        {
            float curSpeed = speed * (float)Time.DeltaTime;

            if (InputManager.IsKeyDown(Key.LeftShift))
                curSpeed *= 5f;
            if (InputManager.IsKeyDown(Key.A))
                gameObject.transform.position -= gameObject.transform.right * curSpeed;
            if (InputManager.IsKeyDown(Key.D))
                gameObject.transform.position += gameObject.transform.right * curSpeed;
            if (InputManager.IsKeyDown(Key.S))
                gameObject.transform.position -= gameObject.transform.forward * curSpeed;
            if (InputManager.IsKeyDown(Key.W))
                gameObject.transform.position += gameObject.transform.forward * curSpeed;
            if (InputManager.IsKeyDown(Key.C))
                gameObject.transform.position -= Vector3.Up * curSpeed;
            if (InputManager.IsKeyDown(Key.Space))
                gameObject.transform.position += Vector3.Up * curSpeed;

            Vector2 mouseDelta = InputManager.GetMouseDelta() / 1000;

            if (!mouseDelta.isZero())
                gameObject.transform.rotation = Quaternion.FromAxisAngle(Vector3.Up, -mouseDelta.x) * Quaternion.FromAxisAngle(gameObject.transform.right, -mouseDelta.y) * gameObject.transform.rotation;
        }
    }
}