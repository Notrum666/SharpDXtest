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
        private static readonly Harmony harmony = new("Profiler");
        private static readonly List<MethodInfo> methodsToPatch = new();
        private static readonly List<ProfilingResult> results = new();

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

        public static void AddProfilingResult(ProfilingResult result)
        {
            results.Add(result);
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
        public long StartTick { get; private set; }
        public long EndTick { get; private set; }

        public int ThreadId { get; private set; }
        public StackTrace StackTrace { get; private set; }

        public long DeltaTicks => EndTick - StartTick;
        public double DeltaMilliseconds => (double)DeltaTicks / Stopwatch.Frequency * 1000;
        public double StartMilliseconds => (double)StartTick / Stopwatch.Frequency * 1000;

        public ProfilingResult(string displayName, long startTickCount)
        {
            DisplayName = displayName;
            StartTick = startTickCount;
            EndTick = Stopwatch.GetTimestamp();

            ThreadId = Environment.CurrentManagedThreadId;
            StackTrace = new StackTrace(2, true);
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
