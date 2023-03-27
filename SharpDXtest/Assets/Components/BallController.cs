using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Engine;
using Engine.BaseAssets.Components;
using Engine.BaseAssets.Components.Colliders;
using LinearAlgebra;
using SharpDX.DirectInput;

namespace SharpDXtest.Assets.Components
{
    class BallController : Component
    {
        public GameObject CameraObject;
        public double Force;
        private bool initialized = false;
        public override void update()
        {
            if (!initialized) 
            {
                gameObject.getComponent<Rigidbody>().OnCollisionBegin += OnCollisionBegin;
                initialized = true;

                Random rng = new Random();
                for (int i = 0; i < 25; i++)
                {
                    GameObject obj = new GameObject();
                    obj.addComponent<Rigidbody>();
                    obj.addComponent<CubeCollider>();
                    obj.addComponent<GravityForce>();
                    Mesh mesh = obj.addComponent<Mesh>();
                    mesh.model = AssetsManager.Models["Cube"];
                    mesh.Material.Albedo = AssetsManager.Textures["Prototype_Light"];
                    double angle = rng.NextDouble() * 2 * Math.PI;
                    double distance = 5 + (1.0 - rng.NextDouble() * rng.NextDouble()) * 35;
                    obj.transform.Position = new Vector3(Math.Cos(angle) * distance, Math.Sin(angle) * distance, 5);
                    double randomScale = 0.5 + rng.NextDouble() * 1.5;
                    obj.transform.LocalScale = new Vector3(randomScale, randomScale, randomScale);

                    GameCore.AddObject(obj);
                }
            }
        }

        public override void fixedUpdate()
        {
            base.fixedUpdate();

            Rigidbody rb = gameObject.getComponent<Rigidbody>();
            rb.addForce(new Vector3(0, 0, -9.8 * rb.Mass));

            if (gameObject.transform.Position.z < -10)
            {
                rb.Velocity = Vector3.Zero;
                rb.AngularVelocity = Vector3.Zero;
                gameObject.transform.Position = new Vector3(0, 0, 5);
            }

            if (InputManager.IsKeyDown(Key.W))
                rb.addForce(CameraObject.transform.Forward.projectOnFlat(Vector3.Up).normalized() * Force);
            if (InputManager.IsKeyDown(Key.S))
                rb.addForce(-CameraObject.transform.Forward.projectOnFlat(Vector3.Up).normalized() * Force);
            if (InputManager.IsKeyDown(Key.A))
                rb.addForce(-CameraObject.transform.Right * Force);
            if (InputManager.IsKeyDown(Key.D))
                rb.addForce(CameraObject.transform.Right * Force);
            if (InputManager.IsKeyPressed(Key.Space))
                rb.addImpulse(Vector3.Up * 5);
        }

        private void OnCollisionBegin(Rigidbody sender, Collider col, Collider other)
        {
            Rigidbody otherRb = other.gameObject.getComponent<Rigidbody>();
            if (otherRb == null || otherRb.IsStatic)
                return;

            if (col.OuterSphereRadius * 1.35 < other.OuterSphereRadius)
                return;

            Vector3 pos = other.gameObject.transform.Position;
            Quaternion quat = other.gameObject.transform.Rotation;
            other.gameObject.transform.setParent(gameObject.transform);
            other.gameObject.transform.Position = pos;
            other.gameObject.transform.Rotation = quat;
            other.gameObject.getComponent<Rigidbody>().Enabled = false;
            other.gameObject.getComponent<Collider>().Enabled = false;

            //other.gameObject.Destroy();
            //gameObject.transform.LocalScale = gameObject.transform.LocalScale + new Vector3(0.2, 0.2, 0.2);

            (col as SphereCollider).Radius += 0.1;
            CameraObject.getComponent<CameraArm>().ArmLength += 0.3;
        }
    }
}
