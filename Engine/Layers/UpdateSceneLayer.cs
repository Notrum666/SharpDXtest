using Engine.BaseAssets.Components;
using System.Collections.Generic;

namespace Engine.Layers
{
    internal class UpdateSceneLayer : Layer
    {
        public override float UpdateOrder => 1;
        public override float InitOrder => 1;

        private Scene currentScene => EngineCore.CurrentScene;
        private static double accumulator = 0.0;

        public override void Init() { }

        public override void Update()
        {
            if (currentScene == null)
                return;

            currentScene.RemoveDestroyedObjects();
            currentScene.ApplyNewObjects();

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
        }

        private void UpdateGameObjects()
        {
            foreach (GameObject obj in currentScene.Objects)
            {
                if (obj.Enabled)
                    obj.Update();
            }
        }

        private void FixedUpdate()
        {
            InputManager.FixedUpdate();

            foreach (GameObject obj in currentScene.Objects)
            {
                if (obj.Enabled)
                    obj.FixedUpdate();
            }

            List<Rigidbody> rigidbodies = new List<Rigidbody>();
            for (int i = 0; i < currentScene.Objects.Count; i++)
            {
                if (!currentScene.Objects[i].Enabled)
                    continue;
                Rigidbody rigidbody = currentScene.Objects[i].GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    foreach (Collider collider in currentScene.Objects[i].GetComponents<Collider>())
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
