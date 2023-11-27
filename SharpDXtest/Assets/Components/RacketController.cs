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
    public class RacketController : Component
    {
        public float speed;
        public GameObject leftRacket, rightRacket;

        public override void Update()
        {
            //if (InputManager.IsKeyPressed(Key.F))
            //    cube.getComponent<Rigidbody>().addImpulse(new Vector3(0.0, 1.0, 0.0));
            //if (InputManager.IsKeyPressed(Key.E))
            //    cube.getComponent<Rigidbody>().addAngularImpulse(new Vector3(0.5, 0.0, 0.0));

            float curSpeed = speed * (float)Time.DeltaTime;

            float delta = 0;
            if (InputManager.IsKeyDown(Key.S))
                delta -= curSpeed;
            if (InputManager.IsKeyDown(Key.W))
                delta += curSpeed;
            leftRacket.Transform.Position = new Vector3(leftRacket.Transform.Position.x,
                                                        leftRacket.Transform.Position.y,
                                                        Math.Min(7.5, Math.Max(-7.5, leftRacket.Transform.Position.z + delta)));

            delta = 0;
            if (InputManager.IsKeyDown(Key.Down))
                delta -= curSpeed;
            if (InputManager.IsKeyDown(Key.Up))
                delta += curSpeed;
            rightRacket.Transform.Position = new Vector3(rightRacket.Transform.Position.x,
                                                         rightRacket.Transform.Position.y,
                                                         Math.Min(7.5, Math.Max(-7.5, rightRacket.Transform.Position.z + delta)));

            //if (InputManager.IsKeyDown(Key.LeftShift))
            //    curSpeed *= 5f;
            //if (InputManager.IsKeyDown(Key.A))
            //    gameObject.transform.localPosition -= gameObject.transform.LocalRight * curSpeed;
            //if (InputManager.IsKeyDown(Key.D))
            //    gameObject.transform.localPosition += gameObject.transform.LocalRight * curSpeed;
            //if (InputManager.IsKeyDown(Key.S))
            //    gameObject.transform.localPosition -= gameObject.transform.LocalForward * curSpeed;
            //if (InputManager.IsKeyDown(Key.W))
            //    gameObject.transform.localPosition += gameObject.transform.LocalForward * curSpeed;
            //if (InputManager.IsKeyDown(Key.C))
            //    // faster
            //    gameObject.transform.localPosition -= (gameObject.transform.Parent == null ? Vector3.Up : (gameObject.transform.Parent.View * Vector4.UnitZ).xyz) * curSpeed;
            //    // simpler
            //    //gameObject.transform.Position -= Vector3.Up * curSpeed;
            //if (InputManager.IsKeyDown(Key.Space))
            //    // faster
            //    gameObject.transform.localPosition += (gameObject.transform.Parent == null ? Vector3.Up : (gameObject.transform.Parent.View * Vector4.UnitZ).xyz) * curSpeed;
            //    // simpler
            //    //gameObject.transform.Position += Vector3.Up * curSpeed;
            //
            //Vector2 mouseDelta = InputManager.GetMouseDelta() / 1000;
            //
            //if (!mouseDelta.isZero())
            //{
            //    // faster
            //    gameObject.transform.localRotation = Quaternion.FromAxisAngle(gameObject.transform.Parent == null ? Vector3.Up :
            //                                                (gameObject.transform.Parent.View * Vector4.UnitZ).xyz, -mouseDelta.x) *
            //                                         Quaternion.FromAxisAngle(gameObject.transform.LocalRight, -mouseDelta.y) *
            //                                         gameObject.transform.localRotation;
            //    // simpler
            //    //gameObject.transform.Rotation = Quaternion.FromAxisAngle(Vector3.Up, -mouseDelta.x) *
            //    //                                     Quaternion.FromAxisAngle(gameObject.transform.right, -mouseDelta.y) *
            //    //                                     gameObject.transform.Rotation;
            //}
        }
    }
}