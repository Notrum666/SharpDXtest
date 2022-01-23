using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using SharpDXtest.BaseAssets.Components;

namespace SharpDXtest
{
    public static class GameCore
    {
        public static Scene CurrentScene { get; private set; }
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
                {
                    needsToBePaused = true;
                    while (!isPaused) ;
                    OnPaused?.Invoke();
                }
                else
                {
                    isPaused = false;
                    OnResumed?.Invoke();
                }
            }
        }
        private static Task renderLoopTask;
        public static event Action OnPaused;
        public static event Action OnResumed;
        public static void Init(Control control)
        {
            // Order of initialization is important, same number means no difference
            GraphicsCore.Init(control); // 1
            SoundCore.Init(); // 1
            Time.Init(); // 1
            InputManager.Init(); // 2

            CurrentScene = AssetsManager.LoadScene("Assets\\Scenes\\Level1.xml");
        }
        public static void Run()
        {
            if (isAlive)
                return;

            isAlive = true;

            renderLoopTask = Task.Run(() =>
            {
                while (isAlive)
                {
                    InputManager.Update();
                    Time.Update();
                    Update();
                    SoundCore.Update();
                    GraphicsCore.Update();

                    if (needsToBePaused)
                    {
                        needsToBePaused = false;
                        isPaused = true;
                        while (isPaused) ;

                        InputManager.Update();
                        Time.Update();
                    }
                }
            });
        }
        public static void Stop()
        {
            if (!isAlive)
                return;

            isAlive = false;
            isPaused = false;
            Task.WaitAll(renderLoopTask);
        }
        public static void Update()
        {
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
            foreach (GameObject obj in CurrentScene.objects)
                if (obj.Enabled)
                    obj.fixedUpdate();

            List<Rigidbody> rigidbodies = new List<Rigidbody>();
            for (int i = 0; i < CurrentScene.objects.Count; i++)
            {
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
