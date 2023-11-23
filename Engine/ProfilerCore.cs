using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.IO;

namespace Engine
{
    public static class ProfilerCore
    {
        public static long MillisecondsToStore = 60 * 1000;

        private static readonly Harmony harmony = new("Profiler");
        private static readonly List<MethodInfo> methodsToPatch = new();

        private static readonly List<ProfilingResult> results = new();
        private static readonly Stack<ProfilingResult> currentStack = new();

        public static void Init()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                IEnumerable<MethodInfo> profiledMethods = assembly.GetTypes()
                    .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                    .Where(m => m.GetCustomAttribute<ProfileMeAttribute>() != null);
                methodsToPatch.AddRange(profiledMethods);
            }

            PatchAll();
        }

        public static void Update()
        {
            long currentTimeMilliseconds = Stopwatch.GetTimestamp() / Stopwatch.Frequency * 1000;
            while (results.Count > 0 && currentTimeMilliseconds - results[0].StartMilliseconds > MillisecondsToStore)
            {
                results.RemoveAt(0);
            }
        }

        public static void AddProfilingResult(ProfilingResult result)
        {
            if (currentStack.TryPeek(out ProfilingResult parent))
            {
                parent.AddChildResult(result);
            }
            else
            {
                results.Add(result);
            }

            currentStack.Push(result);
        }

        public static void FinalizeProfilingResult(ProfilingResult result)
        {
            ProfilingResult stackResult = currentStack.Pop();
            if (result != stackResult)
            {
                throw new ArgumentException($"Trying to finalize result for {result.DisplayName}, while current is {stackResult.DisplayName}");
            }
        }

        public static IReadOnlyList<ProfilingResult> GetResults()
        {
            return results.AsReadOnly();
        }

        private static void PatchAll()
        {
            MethodInfo profilerPrefix = AccessTools.Method(typeof(ProfilerCore), nameof(StartProfiling));
            MethodInfo profilerPostfix = AccessTools.Method(typeof(ProfilerCore), nameof(StopProfiling));

            foreach (MethodInfo method in methodsToPatch)
            {
                harmony.Patch(method, new HarmonyMethod(profilerPrefix), new HarmonyMethod(profilerPostfix));
            }
        }

        private static void StartProfiling(out ProfilingResult __state, MethodInfo __originalMethod)
        {
            ProfileMeAttribute profileAttribute = __originalMethod.GetCustomAttribute<ProfileMeAttribute>();
            __state = new ProfilingResult(profileAttribute.DisplayName);
            AddProfilingResult(__state);
        }

        private static void StopProfiling(ProfilingResult __state)
        {
            __state.StopProfiling();
            FinalizeProfilingResult(__state);
        }

        public static void DumpProfiler(string filePath, string fileName)
        {
            // Set a variable to the Documents path.
            string path = Path.Combine(filePath, fileName);

            using (StreamWriter outputFile = new StreamWriter(path))
            {
                outputFile.WriteLine("{\n\"traceEvents\": [");

                IReadOnlyList<ProfilingResult> results = GetResults();
                DumpResults(results, outputFile, false);

                outputFile.WriteLine("\n]\n}");
            }
        }

        private static void DumpResults(IReadOnlyList<ProfilingResult> results, StreamWriter file, bool skipEndCheck)
        {
            for (int i = 0; i < results.Count; i++)
            {
                ProfilingResult result = results[i];

                DumpResults(result.GetChildResults(), file, true);

                DumpResult(result, file);
                if (skipEndCheck || i != results.Count - 1)
                {
                    file.WriteLine(",");
                }
            }
        }

        private static void DumpResult(ProfilingResult result, StreamWriter file)
        {
            file.WriteLine("{");

            file.WriteLine($"\"cat\": \"default\",");
            file.WriteLine($"\"name\": \"{result.DisplayName}\",");
            file.WriteLine($"\"tid\": {result.ThreadId},");
            file.WriteLine($"\"pid\": 0,");

            file.WriteLine($"\"ph\": \"X\",");
            file.WriteLine($"\"ts\": {result.StartMilliseconds * 1000},");
            file.WriteLine($"\"dur\": {result.DeltaMilliseconds * 1000},");
            file.WriteLine($"\"args\": {{ \"ticks\": {result.DeltaTicks} }}");

            file.Write("}");
        }
    }

    public class ProfilingResult
    {
        public string DisplayName { get; private set; }
        public long StartTick { get; private set; }
        public long EndTick { get; private set; }

        public int ThreadId { get; private set; }
        public StackTrace StackTrace { get; private set; }

        public long DeltaTicks => EndTick - StartTick;
        public double DeltaMilliseconds => (double)DeltaTicks / Stopwatch.Frequency * 1000;
        public double StartMilliseconds => (double)StartTick / Stopwatch.Frequency * 1000;

        private readonly List<ProfilingResult> childResults = new List<ProfilingResult>();

        public ProfilingResult(string displayName)
        {
            DisplayName = displayName;
            StartTick = Stopwatch.GetTimestamp();

            ThreadId = Environment.CurrentManagedThreadId;
            StackTrace = new StackTrace(2, true);
        }

        public void StopProfiling()
        {
            EndTick = Stopwatch.GetTimestamp();
        }

        public void AddChildResult(ProfilingResult result)
        {
            childResults.Add(result);
        }

        public IReadOnlyList<ProfilingResult> GetChildResults()
        {
            return childResults.AsReadOnly();
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ProfileMeAttribute : Attribute
    {
        public string DisplayName { get; private set; }

        public ProfileMeAttribute([CallerMemberName] string displayName = "")
        {
            DisplayName = displayName;
        }
    }
}
