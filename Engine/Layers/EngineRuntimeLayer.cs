using Engine.BaseAssets.Components;

using System.Collections.Generic;

namespace Engine.Layers
{
    internal class EngineRuntimeLayer : Layer
    {
        public override float UpdateOrder => 1;
        public override float InitOrder => 1;

        private Scene CurrentScene => Scene.CurrentScene;

        public override void Prepare()
        {
            SceneManager.TryLoadNextScene();
            if (CurrentScene == null)
                return;

            CurrentScene.ProcessNewObjects();

            InitializeGameObjects(); //Awake
        }

        public override void FixedUpdate()
        {
            if (EngineCore.IsPaused || CurrentScene == null)
                return;

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

        public override void Update()
        {
            if (CurrentScene == null)
                return;

            UpdateGameObjects();
        }

        public override void OnFrameEnded()
        {
            CurrentScene.DestroyPendingObjects();
            SceneManager.TryUnloadCurrentScene();
        }

        private void InitializeGameObjects()
        {
            foreach (GameObject obj in CurrentScene.GameObjects)
                obj.Initialize();
        }

        private void UpdateGameObjects()
        {
            if (EngineCore.IsPaused)
                return;

            foreach (GameObject obj in CurrentScene.GameObjects)
            {
                if (obj.Enabled)
                    obj.Update();
            }
        }
    }
}