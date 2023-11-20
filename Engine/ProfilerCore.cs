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
            Debug.WriteLine($"{result.DisplayName} took {result.Time.Ticks} ticks | {result.Time.TotalMilliseconds} ms to execute");
        }

        private static void PatchAll()
        {
            MethodInfo profilerPrefix = AccessTools.Method(typeof(ProfilerCore), nameof(StartProfiling));
            MethodInfo profilerPostfix = AccessTools.Method(typeof(ProfilerCore), nameof(StopProfiling));

            int patchedMethods = 0;
            foreach (MethodInfo method in _methodsToPatch)
            {
                try
                {
                    Debug.WriteLine($"Trying to patch {method.Name}");
                    _harmony.Patch(method, new HarmonyMethod(profilerPrefix), new HarmonyMethod(profilerPostfix));
                    patchedMethods++;
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Failed to patch {method.Name}, exception: {e}");
                }
            }

            Debug.WriteLine($"Patched {patchedMethods} methods");
        }

        private static void StartProfiling(out Stopwatch __state)
        {
            __state = new Stopwatch();
            __state.Start();
        }

        private static void StopProfiling(Stopwatch __state, MethodInfo __originalMethod)
        {
            __state.Stop();

            ProfileMeAttribute profileAttribute = __originalMethod.GetCustomAttribute<ProfileMeAttribute>();

            ProfilingResult result = new ProfilingResult(profileAttribute.DisplayName, DateTime.Now, __state.Elapsed);

            AddProfilingResult(result);
        }
    }

    public class ProfilingResult
    {
        public string DisplayName { get; private set; }
        public DateTime Occurence { get; private set; }
        public TimeSpan Time { get; private set; }

        public ProfilingResult(string displayName, DateTime occurence, TimeSpan time)
        {
            DisplayName = displayName;
            Occurence = occurence;
            Time = time;
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
