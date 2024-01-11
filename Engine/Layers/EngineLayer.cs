using Engine.BaseAssets.Components;
using System.Collections.Generic;

namespace Engine.Layers
{
    internal class EngineLayer : Layer
    {
        public override float UpdateOrder => 1;
        public override float InitOrder => 1;

        private Scene currentScene => Scene.CurrentScene;
        private static double accumulator = 0.0;

        public override void Update()
        {
            if (Scene.CurrentScene == null)
                return;

            Scene.CurrentScene.ProcessNewObjects();

            InitializeGameObjects();

            accumulator += Time.DeltaTime;

            if (accumulator >= Time.FixedDeltaTime)
            {
                Time.SwitchToFixed();
                do
                {
                    FixedUpdate();
                    accumulator -= Time.FixedDeltaTime;
                } while (accumulator >= Time.FixedDeltaTime);
                Time.SwitchToVariating();
            }

            UpdateGameObjects();

            Scene.CurrentScene.DestroyPendingObjects();
        }

        private static void InitializeGameObjects()
        {
            foreach (GameObject obj in Scene.CurrentScene.GameObjects)
                obj.Initialize();
        }

        private void UpdateGameObjects()
        {
            if (EngineCore.IsPaused)
                return;

            foreach (GameObject obj in currentScene.GameObjects)
            {
                if (obj.Enabled)
                    obj.Update();
            }
        }

        private void FixedUpdate()
        {
            if (EngineCore.IsPaused)
                return;

            InputManager.FixedUpdate();

            foreach (GameObject obj in currentScene.GameObjects)
            {
                if (obj.Enabled)
                    obj.FixedUpdate();
            }

            List<Rigidbody> rigidbodies = new List<Rigidbody>();
            for (int i = 0; i < currentScene.GameObjects.Count; i++)
            {
                if (!currentScene.GameObjects[i].Enabled)
                    continue;
                Rigidbody rigidbody = currentScene.GameObjects[i].GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    foreach (Collider collider in currentScene.GameObjects[i].GetComponents<Collider>())
                        collider.updateData();
                    foreach (Rigidbody otherRigidbody in rigidbodies)
                        rigidbody.solveCollisionWith(otherRigidbody);
                    rigidbodies.Add(rigidbody);
                }
            }
            foreach (Rigidbody rb in rigidbodies)
                rb.updateCollidingPairs();

            foreach (Rigidbody rigidbody in rigidbodies)
                rigidbody.applyChanges();
        }
    }
}
