using System;
using System.Threading;
using System.Threading.Tasks;
using Engine.BaseAssets.Components;

namespace Engine
{
    public static class EngineCore
    {
        private static double accumulator = 0.0;
        private static bool isAlive = false;
        public static bool IsAlive => isAlive;
        private static bool needsToBePaused = false;
        private static bool needsToBeUnpaused = false;
        private static bool isPaused = false;
        public static bool IsPaused
        {
            get => isPaused;
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

                    if (Time.DeltaTime < 1.0 / 144.0)
                        Thread.Sleep((int)((1.0 / 144.0 - Time.DeltaTime) * 1000));
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

        public static void Update()
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

        private static void UpdateGameObjects()
        {
            foreach (GameObject obj in Scene.CurrentScene.GameObjects)
            {
                obj.Update();
            }
        }

        private static void FixedUpdate()
        {
            InputManager.FixedUpdate();

            foreach (GameObject obj in Scene.CurrentScene.GameObjects)
            {
                obj.FixedUpdate();
            }

            Rigidbody[] rigidbodies = Scene.FindComponentsOfType<Rigidbody>();
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                Rigidbody rigidbody = rigidbodies[i];
                foreach (Collider collider in rigidbody.GameObject.GetComponents<Collider>())
                    collider.updateData();
                for (int j = 0; j < i; j++)
                    rigidbody.solveCollisionWith(rigidbodies[j]);
            }

            foreach (Rigidbody rb in rigidbodies)
                rb.updateCollidingPairs();

            foreach (Rigidbody rigidbody in rigidbodies)
                rigidbody.applyChanges();
        }
    }
}