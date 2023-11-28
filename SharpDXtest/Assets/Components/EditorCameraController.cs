using Engine;
using Engine.BaseAssets.Components;

using LinearAlgebra;

using SharpDX.DirectInput;

namespace SharpDXtest.Assets.Components
{
    public class EditorCameraController : Component
    {
        public float speed;

        public override void Update()
        {
            if (!InputManager.IsMouseButtonDown(1))
                return;

            float curSpeed = speed * (float)Time.DeltaTime;

            if (InputManager.IsKeyDown(Key.LeftControl))
                curSpeed /= 5f;
            if (InputManager.IsKeyDown(Key.LeftShift))
                curSpeed *= 5f;
            if (InputManager.IsKeyDown(Key.A))
                GameObject.Transform.LocalPosition -= GameObject.Transform.LocalRight * curSpeed;
            if (InputManager.IsKeyDown(Key.D))
                GameObject.Transform.LocalPosition += GameObject.Transform.LocalRight * curSpeed;
            if (InputManager.IsKeyDown(Key.S))
                GameObject.Transform.LocalPosition -= GameObject.Transform.LocalForward * curSpeed;
            if (InputManager.IsKeyDown(Key.W))
                GameObject.Transform.LocalPosition += GameObject.Transform.LocalForward * curSpeed;
            if (InputManager.IsKeyDown(Key.C))
                // faster
                GameObject.Transform.LocalPosition -= (GameObject.Transform.Parent == null ? Vector3.Up : GameObject.Transform.Parent.View.TransformDirection(Vector3.UnitZ)) * curSpeed;
            // simpler
            //gameObject.transform.Position -= Vector3.Up * curSpeed;
            if (InputManager.IsKeyDown(Key.Space))
                // faster
                GameObject.Transform.LocalPosition += (GameObject.Transform.Parent == null ? Vector3.Up : GameObject.Transform.Parent.View.TransformDirection(Vector3.UnitZ)) * curSpeed;
            // simpler
            //gameObject.transform.Position += Vector3.Up * curSpeed;

            Vector2 mouseDelta = InputManager.GetMouseDelta() / 1000;

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