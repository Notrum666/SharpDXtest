using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpDXtest
{
    public static class Time
    {
        public static double DeltaTime { get; private set; }
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
            DeltaTime = updateWatch.Elapsed.TotalSeconds;
            updateWatch.Restart();

            TotalTime += DeltaTime;
        }
    }
}