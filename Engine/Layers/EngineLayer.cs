using Engine.BaseAssets.Components;

using System.Collections.Generic;

namespace Engine.Layers
{
    internal class EngineLayer : Layer
    {
        public override float UpdateOrder => 1;
        public override float InitOrder => 1;

        private Scene CurrentScene => Scene.CurrentScene;
        private static double accumulator = 0.0;

        public override void Init()
        {
            Time.Init(); // TODO: Move to EngineCore.Run, otherwise may accumulate too high DeltaTime value during other layers initialization
        }

        public override void Update()
        {
            SceneManager.TryLoadNextScene();
            if (Scene.CurrentScene == null || EngineCore.IsPaused)
                return;

            CurrentScene.ProcessNewObjects();

            InitializeGameObjects();

            accumulator += Time.DeltaTime;

            if (accumulator >= Time.FixedDeltaTime) //TODO: Move to EngineCore. Add FixedUpdate to layers...
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
            SceneManager.TryUnloadCurrentScene();
        }

        private void InitializeGameObjects()
        {
            foreach (GameObject obj in Scene.CurrentScene.GameObjects)
                obj.Initialize();
        }

        private void UpdateGameObjects()
        {
            foreach (GameObject obj in CurrentScene.GameObjects)
            {
                if (obj.Enabled)
                    obj.Update();
            }
        }

        private void FixedUpdate()
        {
            InputManager.FixedUpdate();

            foreach (GameObject obj in CurrentScene.GameObjects)
            {
                if (obj.Enabled)
                    obj.FixedUpdate();
            }

            List<Rigidbody> rigidbodies = new List<Rigidbody>();
            for (int i = 0; i < CurrentScene.GameObjects.Count; i++)
            {
                if (!CurrentScene.GameObjects[i].Enabled)
                    continue;
                Rigidbody rigidbody = CurrentScene.GameObjects[i].GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    foreach (Collider collider in CurrentScene.GameObjects[i].GetComponents<Collider>())
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