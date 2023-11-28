using System;
using Engine;
using Engine.BaseAssets.Components;
using LinearAlgebra;

namespace SharpDXtest.Assets.Components
{
    class CameraArm : Component
    {
        public GameObject TargetObject;
        public double ArmLength;
        public double MinYaw;
        public double MaxYaw;
        public double CurYaw;
        public double CurPitch;
        public Vector2 MouseSensitivity;

        public override void Update() { }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            Vector2 mouseDelta = InputManager.GetMouseDelta();
            CurYaw += mouseDelta.y * MouseSensitivity.y;
            CurPitch += mouseDelta.x * MouseSensitivity.x;
            CurYaw = Math.Min(Math.Max(MinYaw, CurYaw), MaxYaw);

            Vector3 arm = Quaternion.FromEuler(new Vector3(-CurYaw / 180.0 * Math.PI, 0, -CurPitch / 180.0 * Math.PI), EulerOrder.YXZ) * new Vector3(0, -ArmLength, 0);
            GameObject.Transform.Position = TargetObject.Transform.Position + arm;
            GameObject.Transform.Rotation = Quaternion.FromEuler(new Vector3((-CurYaw + 10) / 180.0 * Math.PI, 0, -CurPitch / 180.0 * Math.PI), EulerOrder.YXZ);
        }
    }
}