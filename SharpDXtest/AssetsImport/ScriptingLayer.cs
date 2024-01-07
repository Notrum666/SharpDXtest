using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Editor.GameProject;

using Engine;
using Engine.Layers;

using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
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

            foreach (ProjectId projectId in solutionGraph.GetTopologicallySortedProjects())
            {
                Project csProject = solution.GetProject(projectId)!;

                Log(" ----- Project detected! -----");
                Log($"Project name: {csProject.Name}");
                Log($"Assembly name: {csProject.AssemblyName}");
            }

            Log("Recompile succeeded!");
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