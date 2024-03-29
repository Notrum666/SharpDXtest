﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Engine.Layers;

namespace Engine
{
    public static class EngineCore
    {
        private static bool isAlive = false;
        public static bool IsAlive => isAlive;

        private static bool desiredPauseState = true;
        private static bool isPaused = true;
        public static bool IsPaused
        {
            get => isPaused;
            set
            {
                if (isPaused == value)
                    return;

                desiredPauseState = value;
            }
        }
        public static event Action OnPaused;
        public static event Action OnResumed;
        public static event Action OnFrameEnded;

        private static Task loopTask;
        private static List<Layer> layersStack;

        private static double accumulator = 0.0;

        public static void Init(params Layer[] addedLayers)
        {
            Logger.Log(LogType.Info, "Engine initialization");
            // Order of initialization is important, same number means no difference

            layersStack = new List<Layer>
            {
                new ProfilerLayer(),
                new InputLayer(),
                new EngineRuntimeLayer(),
                new SoundLayer(),
                new RenderLayer(),
            };

            if (addedLayers != null)
            {
                layersStack.AddRange(addedLayers);
            }

            foreach (Layer layer in layersStack.OrderBy(x => x.InitOrder))
            {
                layer.Init();
            }

            layersStack.Sort((a, b) => a.UpdateOrder.CompareTo(b.UpdateOrder));

            Logger.Log(LogType.Info, "Engine initialization finished");
        }

        public static async void Run()
        {
            if (isAlive)
                return;

            Time.Init();
            isAlive = true;

            loopTask = Task.Run(() =>
            {
                while (isAlive)
                {
                    if (desiredPauseState != isPaused)
                    {
                        isPaused = desiredPauseState;
                        if (isPaused)
                            OnPaused?.Invoke();
                        else
                            OnResumed?.Invoke();
                    }

                    foreach (Layer layer in layersStack)
                        layer.Prepare();

                    accumulator += Time.DeltaTime;

                    if (accumulator >= Time.FixedDeltaTime)
                    {
                        Time.SwitchToFixed();
                        do
                        {
                            foreach (Layer layer in layersStack)
                                layer.FixedUpdate();
                            accumulator -= Time.FixedDeltaTime;
                        } while (accumulator >= Time.FixedDeltaTime);
                        Time.SwitchToVariating();
                    }

                    foreach (Layer layer in layersStack)
                        layer.Update();

                    Time.Update();

                    foreach (Layer layer in layersStack)
                        layer.OnFrameEnded();

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
    }
}