﻿using System.Diagnostics;

namespace Engine
{
    public static class Time
    {
        private static double deltaTime;
        public static bool IsFixed { get; private set; }
        public static double DeltaTime => IsFixed ? FixedDeltaTime : deltaTime;
        public static double FixedDeltaTime { get; private set; } = 1.0 / 20.0;
        public static double TotalTime { get; private set; }

        private static Stopwatch updateWatch;

        internal static void Init()
        {
            TotalTime = 0;

            updateWatch = new Stopwatch();
            updateWatch.Start();
        }

        public static void Update()
        {
            deltaTime = updateWatch.Elapsed.TotalSeconds;
            updateWatch.Restart();

            TotalTime += DeltaTime;
        }

        public static void SwitchToFixed()
        {
            IsFixed = true;
        }

        public static void SwitchToVariating()
        {
            IsFixed = false;
        }
    }
}