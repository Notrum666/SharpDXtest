using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

using Editor.GameProject;

using Engine;
using Engine.Layers;

using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.MSBuild;

namespace Editor.AssetsImport
{
    internal class ScriptingLayer : Layer
    {
        private const int SafeContextCount = 10;

        private static ScriptingLayer current = null;
        public static ScriptingLayer Current => current ??= new ScriptingLayer();

        public override float UpdateOrder => 0;
        public override float InitOrder => 0;

        public RelayCommand RecompileCommand => recompileCommand ??= new RelayCommand(
            obj => { Recompile(); },
            obj => true
        );

        private RelayCommand recompileCommand;

        private AssemblyLoadContext currentAssemblyContext;
        private readonly List<(string, WeakReference)> unloadingContexts = new List<(string, WeakReference)>();

        public override void Init()
        {
            MSBuildLocator.RegisterDefaults();
        }

        public override void Update() { }

        public void Recompile()
        {
            AssemblyLoadContext oldContext = currentAssemblyContext;

            Task<bool> recompileTask = Task.Run(RecompileAsync);
            recompileTask.Wait();

            if (recompileTask.Result && currentAssemblyContext != oldContext)
                oldContext?.Unload();

            SanitizeUnloadingContexts();
        }

        private async Task<bool> RecompileAsync()
        {
            ProjectViewModel currentProject = ProjectViewModel.Current;
            if (currentProject == null)
            {
                Log("Current project is null.", false);
                return false;
            }

            string solutionPath = Directory.GetFiles(currentProject.FolderPath, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (solutionPath == default)
            {
                Log($"Current project has no solution file in folder {currentProject.FolderPath}.", false);
                return false;
            }

            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            Solution solution = await workspace.OpenSolutionAsync(solutionPath);

            Log($"Loaded solution = {solution} with {solution?.Projects?.Count()} projects at path {solutionPath}");
            AssemblyLoadContext assemblyContext = new AssemblyLoadContext(currentProject.Name, true);
            assemblyContext.Unloading += OnAssemblyContextUnloading;

            ProjectDependencyGraph solutionGraph = solution.GetProjectDependencyGraph();
            foreach (ProjectId projectId in solutionGraph.GetTopologicallySortedProjects())
            {
                Project csProject = solution.GetProject(projectId)!;

                Log(" ----- Project detected! -----");
                Log($"Project name: {csProject.Name}");
                Log($"Assembly name: {csProject.AssemblyName}");

                MemoryStream assemblyStream = await CompileProject(csProject);
                if (assemblyStream == null)
                    continue;

                Assembly asm = assemblyContext.LoadFromStream(assemblyStream);
                assemblyStream.Close();
                Log($"Assembly loaded!");

                Debug.WriteLine($"Assembly types:");
                foreach (Type type in asm.GetTypes())
                {
                    Debug.WriteLine($"{type.FullName}");
                }
            }

            if (!assemblyContext.Assemblies.Any())
            {
                Log($"Loaded 0 assemblies for solution at {currentProject.FolderPath}.", false);
                assemblyContext.Unload();
                return false;
            }

            Log("Recompile succeeded!");
            currentAssemblyContext = assemblyContext;
            return true;
        }

        private async Task<MemoryStream> CompileProject(Project csProject)
        {
            Compilation compilation = await csProject.GetCompilationAsync();
            if (compilation == null)
            {
                Log($"Could not compile project {csProject.Name}. SupportsCompilation = {csProject.SupportsCompilation}", false);
                return null;
            }
            Log($"Compiled to path: {csProject.OutputFilePath}");

            MemoryStream stream = new MemoryStream();
            EmitResult result = compilation.Emit(stream);
            stream.Position = 0;

            if (!result.Success)
            {
                foreach (Diagnostic diagnostic in result.Diagnostics)
                    if (diagnostic.Severity == DiagnosticSeverity.Error)
                        Debug.WriteLine("Error: {0}", diagnostic.GetMessage());
                return null;
            }

            return stream;
        }

        private void SanitizeUnloadingContexts()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            unloadingContexts.RemoveAll(x => !x.Item2.IsAlive);
            Logger.Log(LogType.Info, $"Unloading contexts count = {unloadingContexts.Count}");
            
            if (unloadingContexts.Count > SafeContextCount)
            {
                string contextsNames = string.Join("; ", unloadingContexts.Select(x => x.Item1));
                Logger.Log(LogType.Warning, $"Too many unloading contexts({unloadingContexts.Count}): {contextsNames}");
            }
        }

        private void OnAssemblyContextUnloading(AssemblyLoadContext context)
        {
            string contextName = context.Name;
            WeakReference contextRef = new WeakReference(context);

            unloadingContexts.Add((contextName, contextRef));
        }

        private void Log(string message, bool success = true)
        {
            if (success)
            {
                Logger.Log(LogType.Info, $"{message}");
                Debug.WriteLine($"ScriptingLayer - success: {message}");
            }
            else
            {
                Logger.Log(LogType.Error, $"Recompile failed! Reason: {message}");
                Debug.WriteLine($"ScriptingLayer - failed: {message}");
            }
        }
    }
}