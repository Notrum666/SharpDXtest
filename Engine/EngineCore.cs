using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Engine.BaseAssets.Components;
using Engine.Layers;

namespace Engine
{
    public static class EngineCore
    {
        public static Scene CurrentScene { get; set; }
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

        private static List<Layer> layersStack;

        public static void Init(nint HWND, int width, int height, IEnumerable<Layer> addedLayers = null)
        {
            Logger.Log(LogType.Info, "Engine initialization");

            // Order of initialization is important, same number means no difference
            ProfilerCore.Init();
            GraphicsCore.Init(HWND, width, height); // 1
            SoundCore.Init(); // 1
            Time.Init(); // 1
            InputManager.Init(); // 2

            layersStack = new List<Layer>
            {
                new InputLayer(),
                new ProfilerLayer(), 
                new RenderSceneLayer(),
                new SoundLayer(),
                new UpdateSceneLayer()
            };

            if(addedLayers != null)
            {
                layersStack.AddRange(addedLayers);
            }

            foreach(Layer layer in layersStack.OrderBy(x => x.InitOrder))
            {
                layer.Init();
            }

            layersStack.Sort((a, b) => a.UpdateOrder.CompareTo(b.UpdateOrder));

            Logger.Log(LogType.Info, "Engine initialization finished");
        }

        public static async void Run()
        {
            if (isAlive)
                throw new Exception("EngineCore already ran");

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

                    foreach(Layer layer in layersStack)
                    {
                        layer.Update();
                    }

                    Time.Update();

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
                throw new Exception("EngineCore already stopped");

            isAlive = false;
            isPaused = false;
            Task.WaitAll(loopTask);

            GraphicsCore.Dispose();
            SoundCore.Dispose();
        }
    }
}