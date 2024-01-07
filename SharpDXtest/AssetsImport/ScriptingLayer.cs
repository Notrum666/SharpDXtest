using System;
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
        private RelayCommand recompileCommand;

        public RelayCommand RecompileCommand => recompileCommand ??= new RelayCommand(
            obj => { Recompile(); },
            obj => true
        );

        private static ScriptingLayer current = null;
        public static ScriptingLayer Current => current ??= new ScriptingLayer();

        public override float UpdateOrder => 0;
        public override float InitOrder => 0;

        public override void Init()
        {
            MSBuildLocator.RegisterDefaults();
        }

        public override void Update() { }

        public void Recompile()
        {
            Task.Run(RecompileAsync).Wait();
        }

        private async Task RecompileAsync()
        {
            ProjectViewModel currentProject = ProjectViewModel.Current;
            if (currentProject == null)
            {
                Log("Current project is null.", false);
                return;
            }

            string solutionPath = Directory.GetFiles(currentProject.FolderPath, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (solutionPath == default)
            {
                Log($"Current project has no solution file in folder {currentProject.FolderPath}.", false);
                return;
            }

            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            Solution solution = await workspace.OpenSolutionAsync(solutionPath);

            Log($"Loaded solution = {solution} with {solution?.Projects?.Count()} projects at path {solutionPath}");

            ProjectDependencyGraph solutionGraph = solution.GetProjectDependencyGraph();
            // AssemblyLoadContext context = new AssemblyLoadContext(currentProject.Name, true);

            foreach (ProjectId projectId in solutionGraph.GetTopologicallySortedProjects())
            {
                Project csProject = solution.GetProject(projectId)!;

                Log(" ----- Project detected! -----");
                Log($"Project name: {csProject.Name}");
                Log($"Assembly name: {csProject.AssemblyName}");

                MemoryStream assemblyStream = await CompileProject(csProject);
                if (assemblyStream == null)
                    continue;

                Assembly asm = Assembly.Load(assemblyStream.ToArray());
                Log($"Assembly loaded!");
                
                Debug.WriteLine($"Assembly types:");
                foreach (Type type in asm.GetTypes())
                {
                    Debug.WriteLine($"{type.FullName}");
                }
            }

            Log("Recompile succeeded!");
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