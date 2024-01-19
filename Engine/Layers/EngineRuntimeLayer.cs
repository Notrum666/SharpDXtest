using Engine.BaseAssets.Components;

using System;
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

            foreach (GameObject obj in CurrentScene.GameObjects)
            {
                try
                {
                    obj.Initialize();
                }
                catch (Exception e)
                {
                    Logger.Log(LogType.Error, $"Error during GameObject initialization, GameObject name: {obj.Name}, error: {e.Message}");
                }
            }
        }

        public override void FixedUpdate()
        {
            if (EngineCore.IsPaused || CurrentScene == null)
                return;

            foreach (GameObject obj in CurrentScene.GameObjects)
            {
                if (obj.Enabled)
                    try
                    {
                        obj.FixedUpdate();
                    }
                    catch (Exception e)
                    {
                        Logger.Log(LogType.Error, $"Error during GameObject's FixedUpdate, GameObject name: {obj.Name}, error: {e.Message}");
                    }
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
                        collider.UpdateData();
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
            }
            foreach (Rigidbody rb in rigidbodies)
                rb.UpdateCollidingPairs();

            foreach (Rigidbody rigidbody in rigidbodies)
                rigidbody.ApplyChanges();
        }

        public override void Update()
        {
            if (CurrentScene == null || EngineCore.IsPaused)
                return;

            foreach (GameObject obj in CurrentScene.GameObjects)
            {
                if (obj.Enabled)
                    try
                    {
                        obj.Update();
                    }
                    catch (Exception e)
                    {
                        Logger.Log(LogType.Error, $"Error during GameObject's Update, GameObject name: {obj.Name}, error: {e.Message}");
                    }
            }
        }

        public override void OnFrameEnded()
        {
            CurrentScene?.DestroyPendingObjects();
            SceneManager.TryUnloadCurrentScene();
        }
    }
}