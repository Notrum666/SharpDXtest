using Engine.BaseAssets.Components;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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

            foreach (GameObject obj in CurrentScene.GameObjects)
                obj.Initialize();
        }

        public override void FixedUpdate()
        {
            if (EngineCore.IsPaused || CurrentScene == null)
                return;

            foreach (GameObject obj in CurrentScene.GameObjects)
                if (obj.Enabled)
                    obj.FixedUpdate();

            List<Rigidbody> rigidbodies = new List<Rigidbody>();
            List<Collider> allColliders = new List<Collider>();
            List<Collider> hangingColliders = new List<Collider>();
            for (int i = 0; i < CurrentScene.GameObjects.Count; i++)
            {
                if (!CurrentScene.GameObjects[i].Enabled)
                    continue;

                IEnumerable<Collider> curColliders = CurrentScene.GameObjects[i].GetComponents<Collider>();
                if (curColliders.Count() == 0)
                    continue;
                allColliders.AddRange(curColliders);
                foreach (Collider collider in curColliders)
                    collider.UpdateData();

                Rigidbody rigidbody = CurrentScene.GameObjects[i].GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    foreach (Rigidbody otherRigidbody in rigidbodies)
                        try
                        {
                            rigidbody.SolveCollisionWith(otherRigidbody);
                        }
                        catch (Exception e)
                        {
                            Logger.Log(LogType.Error, $"Error during collision solving for GameObjects {rigidbody.GameObject.Name} and " +
                                $"{otherRigidbody.GameObject.Name}, error: {e.Message}");
                        }
                    rigidbodies.Add(rigidbody);
                }
                else
                {
                    foreach (Collider collider in curColliders)
                        foreach (Collider other in hangingColliders)
                            if (!collider.IsTrigger || !other.IsTrigger)
                                collider.GetCollisionExitVector(other, out _, out _, out _);
                    hangingColliders.AddRange(curColliders);
                }
            }
            foreach (Rigidbody rb in rigidbodies)
                rb.UpdateCollidingPairs();
            foreach (Collider col in allColliders)
                col.UpdateCollidingColliders();


            foreach (Rigidbody rigidbody in rigidbodies)
                rigidbody.ApplyChanges();
        }

        public override void Update()
        {
            if (CurrentScene == null || EngineCore.IsPaused)
                return;

            foreach (GameObject obj in CurrentScene.GameObjects)
                if (obj.Enabled)
                    obj.Update();

            Coroutine.Update();
        }

        public override void OnFrameEnded()
        {
            CurrentScene?.DestroyPendingObjects();
            SceneManager.TryUnloadCurrentScene();
        }
    }
}