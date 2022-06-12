using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Interop;
using Engine.BaseAssets.Components;

namespace Engine
{
    public static class GameCore
    {
        public static Scene CurrentScene { get; set; }
        private static double accumulator = 0.0;
        private static bool isAlive = false;
        public static bool IsAlive { get => isAlive; }
        private static bool needsToBePaused = false;
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
                    isPaused = false;
            }
        }
        private static Task loopTask;
        public static event Action OnPaused;
        public static event Action OnResumed;
        public static event Action OnFrameEnded;
        public static void Init(IntPtr HWND, int width, int height)
        {
            // Order of initialization is important, same number means no difference
            GraphicsCore.Init(HWND, width, height); // 1
            SoundCore.Init(); // 1
            Time.Init(); // 1
            InputManager.Init(); // 2
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
                    Update();
                    SoundCore.Update();
                    GraphicsCore.Update();

                    OnFrameEnded?.Invoke();

                    if (needsToBePaused)
                    {
                        needsToBePaused = false;
                        isPaused = true;
                        OnPaused?.Invoke();
                        while (isPaused && IsAlive) ;

                        if (IsAlive)
                        {
                            InputManager.Update();
                            Time.Update();
                            OnResumed?.Invoke();
                        }
                    }
                }
            });
            try
            {
                await loopTask;
            }
            catch (Exception e)
            {
                throw e;
            }
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
            if (CurrentScene == null)
                return;

            update();

            accumulator += Time.DeltaTime;

            Time.SwitchToFixed();
            while (accumulator >= Time.FixedDeltaTime)
            {
                fixedUpdate();
                accumulator -= Time.FixedDeltaTime;
            }
            Time.SwitchToVariating();
        }
        private static void update()
        {
            foreach (GameObject obj in CurrentScene.objects)
                if (obj.Enabled)
                    obj.update();
        }
        private static void fixedUpdate()
        {
            InputManager.FixedUpdate();

            foreach (GameObject obj in CurrentScene.objects)
                if (obj.Enabled)
                    obj.fixedUpdate();

            List<Rigidbody> rigidbodies = new List<Rigidbody>();
            for (int i = 0; i < CurrentScene.objects.Count; i++)
            {
                if (!CurrentScene.objects[i].Enabled)
                    continue;
                Rigidbody rigidbody = CurrentScene.objects[i].getComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    foreach (Rigidbody otherRigidbody in rigidbodies)
                        rigidbody.solveCollisionWith(otherRigidbody);
                    rigidbodies.Add(rigidbody);
                }
            }

            foreach (Rigidbody rigidbody in rigidbodies)
                rigidbody.applyChanges();
        }
    }
}
