using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Interop;
using Engine.BaseAssets.Components;

namespace Engine
{
    public static class EngineCore
    {
        public static Scene CurrentScene { get; set; }
        private static List<GameObject> newObjects = new List<GameObject>();
        private static double accumulator = 0.0;
        private static bool isAlive = false;
        public static bool IsAlive { get => isAlive; }
        private static bool needsToBePaused = false;
        private static bool needsToBeUnpaused = false;
        private static bool isPaused = false;
        public static bool IsPaused 
        {
            get
            {
                return isPaused;
            }
            set
            {
                if (isPaused == value || !isAlive)
                    return;

                if (value)
                    needsToBePaused = true;
                else
                    needsToBeUnpaused = true;
            }
        }
        private static Task loopTask;
        public static event Action OnPaused;
        public static event Action OnResumed;
        public static event Action OnFrameEnded;
        public static void Init(nint HWND, int width, int height)
        {
            Logger.Log(LogType.Info, "Engine initialization");
            // Order of initialization is important, same number means no difference
            ProfilerCore.Init();
            GraphicsCore.Init(HWND, width, height); // 1
            SoundCore.Init(); // 1
            Time.Init(); // 1
            InputManager.Init(); // 2
            Logger.Log(LogType.Info, "Engine initialization finished");
        }
        public static async void Run()
        {
            if (isAlive)
                return;

            isAlive = true;

            loopTask = Task.Run(() =>
            {
                while (isAlive)
                {
                    InputManager.Update();
                    Time.Update();
                    if (!isPaused)
                    {
                        Update();
                        SoundCore.Update();
                    }
                    GraphicsCore.Update();
                    ProfilerCore.Update();

                    OnFrameEnded?.Invoke();

                    if (needsToBePaused)
                    {
                        needsToBePaused = false;
                        isPaused = true;
                        OnPaused?.Invoke();
                    }

                    if (needsToBeUnpaused)
                    {
                        needsToBeUnpaused = false;
                        isPaused = false;
                        OnResumed?.Invoke();
                    }

                    if (Time.DeltaTime < 1.0/144.0)
                    {
                        Thread.Sleep((int)((1.0 / 144.0 - Time.DeltaTime) * 1000));
                    }
                }
            });
            await loopTask;
        }
        public static void Stop()
        {
            if (!isAlive)
                return;

            isAlive = false;
            isPaused = false;
            Task.WaitAll(loopTask);

            GraphicsCore.Dispose();
            SoundCore.Dispose();
        }
        public static void AddObject(GameObject obj)
        {
            if (CurrentScene == null || CurrentScene.objects.Contains(obj) || newObjects.Contains(obj))
                return;

            newObjects.Add(obj);
        }
        public static void Update()
        {
            if (CurrentScene == null)
                return;

            CurrentScene.objects.RemoveAll(obj => obj.PendingDestroy);
            CurrentScene.objects.AddRange(newObjects);
            newObjects.Clear();

            initialize();

            accumulator += Time.DeltaTime;

            if (accumulator >= Time.FixedDeltaTime)
            {
                Time.SwitchToFixed();
                do
                {
                    fixedUpdate();
                    accumulator -= Time.FixedDeltaTime;
                }
                while (accumulator >= Time.FixedDeltaTime);
                Time.SwitchToVariating();
            }

            update();
        }
        private static void initialize()
        {
            foreach (GameObject obj in CurrentScene.objects)
                obj.Initialize();
        }
        private static void update()
        {
            foreach (GameObject obj in CurrentScene.objects)
                if (obj.Enabled)
                    obj.Update();
        }
        private static void fixedUpdate()
        {
            InputManager.FixedUpdate();

            foreach (GameObject obj in CurrentScene.objects)
                if (obj.Enabled)
                    obj.FixedUpdate();

            List<Rigidbody> rigidbodies = new List<Rigidbody>();
            for (int i = 0; i < CurrentScene.objects.Count; i++)
            {
                if (!CurrentScene.objects[i].Enabled)
                    continue;
                Rigidbody rigidbody = CurrentScene.objects[i].GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    foreach (Collider collider in CurrentScene.objects[i].GetComponents<Collider>())
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
