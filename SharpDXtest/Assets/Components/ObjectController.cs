using Engine;
using Engine.BaseAssets.Components;

using LinearAlgebra;

using SharpDX.DirectInput;

namespace SharpDXtest.Assets.Components
{
    class ObjectController : Component
    {
        public double Speed { get; set; } = 5.0;

        public override void Update()
        {
            if (InputManager.IsKeyDown(Key.Up))
                GameObject.Transform.Position += Vector3.Up * Speed * Time.DeltaTime;
            if (InputManager.IsKeyDown(Key.Down))
                GameObject.Transform.Position -= Vector3.Up * Speed * Time.DeltaTime;
            if (InputManager.IsKeyDown(Key.Left))
                GameObject.Transform.Position -= Vector3.Right * Speed * Time.DeltaTime;
            if (InputManager.IsKeyDown(Key.Right))
                GameObject.Transform.Position += Vector3.Right * Speed * Time.DeltaTime;
        }
    }
}