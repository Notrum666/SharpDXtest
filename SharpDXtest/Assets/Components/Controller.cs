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
        public GameObject cube;
        public override void update()
        {
            if (InputManager.IsKeyDown(Key.F))
                cube.getComponent<Rigidbody>().addForce(new Vector3(0.0, 1.0, 0.0));

            float curSpeed = speed * (float)Time.DeltaTime;

            if (InputManager.IsKeyDown(Key.LeftShift))
                curSpeed *= 5f;
            if (InputManager.IsKeyDown(Key.A))
                gameObject.transform.localPosition -= gameObject.transform.localRight * curSpeed;
            if (InputManager.IsKeyDown(Key.D))
                gameObject.transform.localPosition += gameObject.transform.localRight * curSpeed;
            if (InputManager.IsKeyDown(Key.S))
                gameObject.transform.localPosition -= gameObject.transform.localForward * curSpeed;
            if (InputManager.IsKeyDown(Key.W))
                gameObject.transform.localPosition += gameObject.transform.localForward * curSpeed;
            if (InputManager.IsKeyDown(Key.C))
                // faster
                gameObject.transform.localPosition -= (gameObject.transform.Parent == null ? Vector3.Up : (gameObject.transform.Parent.view * Vector4.UnitZ).xyz) * curSpeed;
                // simpler
                //gameObject.transform.Position -= Vector3.Up * curSpeed;
            if (InputManager.IsKeyDown(Key.Space))
                // faster
                gameObject.transform.localPosition += (gameObject.transform.Parent == null ? Vector3.Up : (gameObject.transform.Parent.view * Vector4.UnitZ).xyz) * curSpeed;
                // simpler
                //gameObject.transform.Position += Vector3.Up * curSpeed;

            Vector2 mouseDelta = InputManager.GetMouseDelta() / 1000;

            if (!mouseDelta.isZero())
            {
                // faster
                gameObject.transform.localRotation = Quaternion.FromAxisAngle(gameObject.transform.Parent == null ? Vector3.Up :
                                                            (gameObject.transform.Parent.view * Vector4.UnitZ).xyz, -mouseDelta.x) *
                                                     Quaternion.FromAxisAngle(gameObject.transform.localRight, -mouseDelta.y) *
                                                     gameObject.transform.localRotation;
                // simpler
                //gameObject.transform.Rotation = Quaternion.FromAxisAngle(Vector3.Up, -mouseDelta.x) *
                //                                     Quaternion.FromAxisAngle(gameObject.transform.right, -mouseDelta.y) *
                //                                     gameObject.transform.Rotation;
            }
        }
    }
}