using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Engine
{
    public static class ProfilerCore
    {
        private static readonly Harmony _harmony = new("Profiler");
        private static readonly List<MethodInfo> _methodsToPatch = new();
        private static readonly List<ProfilingResult> _results = new();

        public static void Init()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                IEnumerable<MethodInfo> profiledMethods = assembly.GetTypes()
                    .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                    .Where(m => m.GetCustomAttribute<ProfileMeAttribute>() != null);
                _methodsToPatch.AddRange(profiledMethods);
            }

            Debug.WriteLine($"Found {_methodsToPatch.Count} profileable methods in {assemblies.Length} assemblies");

            PatchAll();

            Debug.WriteLine("Profiler initialized");
        }

        public static void AddProfilingResult(ProfilingResult result)
        {
            _results.Add(result);
            Debug.WriteLine($"{result.DisplayName} took {result.DeltaTicks} ticks | {result.DeltaMilliseconds} ms to execute");
        }

        private static void PatchAll()
        {
            MethodInfo profilerPrefix = AccessTools.Method(typeof(ProfilerCore), nameof(StartProfiling));
            MethodInfo profilerPostfix = AccessTools.Method(typeof(ProfilerCore), nameof(StopProfiling));

            int patchedMethods = 0;
            foreach (MethodInfo method in _methodsToPatch)
            {
                _harmony.Patch(method, new HarmonyMethod(profilerPrefix), new HarmonyMethod(profilerPostfix));
                patchedMethods++;
            }

            Debug.WriteLine($"Patched {patchedMethods} methods");
        }

        private static void StartProfiling(out long __state)
        {
            __state = Stopwatch.GetTimestamp();
        }

        private static void StopProfiling(long __state, MethodInfo __originalMethod)
        {
            ProfileMeAttribute profileAttribute = __originalMethod.GetCustomAttribute<ProfileMeAttribute>();

            ProfilingResult result = new ProfilingResult(profileAttribute.DisplayName, __state);

            AddProfilingResult(result);
        }
    }

    public class ProfilingResult
    {
        public string DisplayName { get; private set; }
        public long StartTickCount { get; private set; }
        public long EndTickCount { get; private set; }

        public long DeltaTicks => EndTickCount - StartTickCount;
        public float DeltaSeconds => (float)DeltaTicks / Stopwatch.Frequency;
        public float DeltaMilliseconds => DeltaSeconds * 1000;

        public ProfilingResult(string displayName, long startTickCount)
        {
            DisplayName = displayName;
            StartTickCount = startTickCount;
            EndTickCount = Stopwatch.GetTimestamp();
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
