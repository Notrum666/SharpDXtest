using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

using Editor.GameProject;

using Engine;
using Engine.BaseAssets.Components;
using Engine.Layers;

using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        public bool IsCompilationRelevant;
        public ReadOnlyDictionary<string, List<Type>> FilesToTypesMap { get; private set; }

        private readonly Dictionary<string, List<Type>> filesToTypesMap = new Dictionary<string, List<Type>>();

        private AssemblyLoadContext currentAssemblyContext;
        private readonly List<(string, WeakReference)> unloadingContexts = new List<(string, WeakReference)>();

        public override void Init()
        {
            MSBuildLocator.RegisterDefaults();

            IsCompilationRelevant = false;
            FilesToTypesMap = new ReadOnlyDictionary<string, List<Type>>(filesToTypesMap);
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
            IsCompilationRelevant = true;
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
            workspace.WorkspaceChanged += WorkspaceOnWorkspaceChanged;

            Log($"Loaded solution = {solution} with {solution?.Projects?.Count()} projects at path {solutionPath}");
            AssemblyLoadContext assemblyContext = new AssemblyLoadContext(currentProject.Name, true);
            Dictionary<string, List<Type>> filesToTypes = new Dictionary<string, List<Type>>();

            assemblyContext.Unloading += OnAssemblyContextUnloading;

            ProjectDependencyGraph solutionGraph = solution.GetProjectDependencyGraph();
            foreach (ProjectId projectId in solutionGraph.GetTopologicallySortedProjects())
            {
                Project csProject = solution.GetProject(projectId)!;
                Log(" ----- Project detected! -----");
                Log($"Project name: {csProject.Name}");
                Log($"Assembly name: {csProject.AssemblyName}");

                (MemoryStream assemblyStream, Dictionary<string, List<string>> filesToTypeNamesMap) = await CompileProject(csProject);
                if (assemblyStream == null)
                    continue;

                Assembly asm = assemblyContext.LoadFromStream(assemblyStream);
                assemblyStream.Close();
                Log($"Assembly loaded!");

                foreach (string file in filesToTypeNamesMap.Keys)
                {
                    if (!filesToTypes.ContainsKey(file))
                        filesToTypes[file] = new List<Type>();

                    List<string> typeNames = filesToTypeNamesMap[file];
                    foreach (string typeName in typeNames)
                    {
                        Type type = asm.GetType(typeName);
                        if (type != null)
                            filesToTypes[file].Add(type);
                    }
                }

                Debug.WriteLine($"Assembly types:");
                foreach (string file in filesToTypes.Keys)
                {
                    Debug.WriteLine($"{file}:");
                    foreach (Type type in filesToTypes[file])
                    {
                        Debug.WriteLine($" - {type}");
                    }
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
            filesToTypesMap.Clear();
            filesToTypesMap.AddRange(filesToTypes);
            return true;
        }

        private void WorkspaceOnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            // e.Kind ==
            throw new NotImplementedException();
        }

        private async Task<(MemoryStream stream, Dictionary<string, List<string>> filesToTypeNamesMap)> CompileProject(Project csProject)
        {
            Compilation compilation = await csProject.GetCompilationAsync();
            if (compilation == null)
            {
                Log($"Could not compile project {csProject.Name}. SupportsCompilation = {csProject.SupportsCompilation}", false);
                return (null, null);
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
                return (null, null);
            }

            // string componentTypeName = typeof(Component).FullName;
            // INamedTypeSymbol componentTypeSymbol = compilation.GetTypeByMetadataName(componentTypeName!)!;
            SymbolDisplayFormat displayFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
            Dictionary<string, List<string>> filesToTypeNamesMap = new Dictionary<string, List<string>>();
            foreach (INamedTypeSymbol typeSymbol in GetNamedTypeSymbols(compilation))
            {
                Location typeLocation = typeSymbol.Locations.FirstOrDefault(x => x.Kind == LocationKind.SourceFile);
                if (typeLocation == default)
                    continue;

                string filePath = typeLocation.SourceTree!.FilePath;
                if (!filesToTypeNamesMap.ContainsKey(filePath))
                    filesToTypeNamesMap[filePath] = new List<string>();

                // genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters
                filesToTypeNamesMap[filePath].Add(typeSymbol.ToDisplayString(displayFormat));
                // Debug.WriteLine($"found typeSymbol = {typeSymbol.ToString()}");
                // Debug.WriteLine($"namespace = {typeSymbol.ContainingNamespace}");
                // Debug.WriteLine($"namespace = {typeSymbol.ContainingNamespace?.ContainingNamespace}");
                // Debug.WriteLine($"path = {typeSymbol.Locations[0].SourceTree?.FilePath}");
                // var baseType = typeSymbol.BaseType;
                // while (baseType != null)
                // {
                //     Debug.WriteLine($"baseType = {baseType}");
                //     Debug.WriteLine($"equals = {SymbolEqualityComparer.Default.Equals(baseType, componentTypeSymbol)}");
                //     baseType = baseType.BaseType;
                // }
            }

            return (stream, filesToTypeNamesMap);
        }

        private static IEnumerable<INamedTypeSymbol> GetNamedTypeSymbols(Compilation compilation)
        {
            Stack<INamespaceSymbol> stack = new Stack<INamespaceSymbol>();
            stack.Push(compilation.Assembly.GlobalNamespace);

            while (stack.Count > 0)
            {
                INamespaceSymbol @namespace = stack.Pop();

                foreach (INamespaceOrTypeSymbol member in @namespace.GetMembers())
                {
                    switch (member)
                    {
                        case INamespaceSymbol memberAsNamespace:
                            stack.Push(memberAsNamespace);
                            break;
                        case INamedTypeSymbol memberAsNamedTypeSymbol:
                            yield return memberAsNamedTypeSymbol;
                            break;
                    }
                }
            }
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