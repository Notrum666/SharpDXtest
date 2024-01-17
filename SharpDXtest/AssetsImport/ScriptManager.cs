using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.Loader;
using System.Threading.Tasks;

using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.MSBuild;

using Engine;
using System.Diagnostics;

namespace Editor.AssetsImport
{
    public static class ScriptManager
    {
        private const int SafeContextCount = 10;

        public static bool IsCompilationRelevant { get; private set; }
        public static Action OnCodeRecompiled { get; set; }

        internal static ReadOnlyDictionary<string, List<Type>> FilesToTypesMap { get; }

        private static readonly Dictionary<string, List<Type>> filesToTypesMap = new Dictionary<string, List<Type>>();

        private static readonly FileSystemWatcher filesWatcher;
        private static readonly MSBuildWorkspace currentWorkspace;

        private static readonly List<(string, WeakReference)> unloadingContexts = new List<(string, WeakReference)>();
        private static AssemblyLoadContext currentAssemblyContext;

        static ScriptManager()
        {
            MSBuildLocator.RegisterDefaults();

            IsCompilationRelevant = false;
            FilesToTypesMap = new ReadOnlyDictionary<string, List<Type>>(filesToTypesMap);


            var properties = new Dictionary<string, string>()
            {
                { "CheckForSystemRuntimeDependency", "true" },
                { "DesignTimeBuild", "true" },
                { "BuildingInsideVisualStudio", "true" },
                { "AlwaysCompileMarkupFilesInSeparateDomain", "false" }
            };
            currentWorkspace = MSBuildWorkspace.Create(properties);

            filesWatcher = new FileSystemWatcher();
            filesWatcher.EnableRaisingEvents = false;
            filesWatcher.IncludeSubdirectories = true;
            filesWatcher.Filter = "*.cs";
            filesWatcher.NotifyFilter = NotifyFilters.CreationTime
                                        | NotifyFilters.DirectoryName
                                        | NotifyFilters.FileName
                                        | NotifyFilters.LastWrite;

            filesWatcher.Changed += OnFilesChanged;
        }

        public static void SetResourceAssembly(Assembly assembly)
        {
            //var _resourceAssemblyField = typeof(Application).GetField("_resourceAssembly", BindingFlags.Static | BindingFlags.NonPublic);
            ////if (_resourceAssemblyField != null)
            //_resourceAssemblyField.SetValue(null, assembly);

            //var resourceAssemblyProperty = typeof(BaseUriHelper).GetProperty("ResourceAssembly", BindingFlags.Static | BindingFlags.NonPublic);
            ////if (resourceAssemblyProperty != null)
            //resourceAssemblyProperty.SetValue(null, assembly);
        }

        public static void Recompile()
        {
            filesWatcher.EnableRaisingEvents = false;
            AssemblyLoadContext oldContext = currentAssemblyContext;

            Task<bool> recompileTask = Task.Run(RecompileAsync);
            recompileTask.Wait();

            if (!recompileTask.Result || currentAssemblyContext == oldContext)
            {
                return;
            }

            oldContext?.Unload();
            SanitizeUnloadingContexts();

            IsCompilationRelevant = true;
            filesWatcher.Path = AssetsRegistry.ContentFolderPath;
            filesWatcher.EnableRaisingEvents = true;

            currentAssemblyContext.EnterContextualReflection();
            OnCodeRecompiled?.Invoke();

            //Debug.WriteLine("==========");
            //Debug.WriteLine($"loaded type = {filesToTypesMap.Values.First().First().Name}");
            //Debug.WriteLine($"loaded type = {filesToTypesMap.Values.First().First().AssemblyQualifiedName}");
            //Debug.WriteLine($"loaded type = {filesToTypesMap.Values.First().First().GetFields().Length}");

            //Debug.WriteLine($"context = {AssemblyLoadContext.CurrentContextualReflectionContext?.Name}");
            //Debug.WriteLine($"loaded GetType = {Type.GetType("TestProjectComponent")?.Name}");
            //Debug.WriteLine($"loaded GetType = {Type.GetType("TestProject.TestProjectComponent,TestProject")?.Name}");
            //Debug.WriteLine("==========");
        }

        private static async Task<bool> RecompileAsync()
        {
            ProjectViewModel currentProject = ProjectViewModel.Current;
            if (currentProject == null)
            {
                Logger.Log(LogType.Error, $"Recompile failed! Current project is null.");
                return false;
            }

            string solutionPath = Directory.GetFiles(currentProject.FolderPath, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (solutionPath == default)
            {

                Logger.Log(LogType.Error, $"Recompile failed! Current project has no solution file in folder {currentProject.FolderPath}");
                return false;
            }

            currentWorkspace.CloseSolution();
            Solution solution = await currentWorkspace.OpenSolutionAsync(solutionPath);

            AssemblyLoadContext assemblyContext = new AssemblyLoadContext(currentProject.Name, true);
            Dictionary<string, List<Type>> filesToTypes = new Dictionary<string, List<Type>>();

            assemblyContext.Unloading += OnAssemblyContextUnloading;

            ProjectDependencyGraph solutionGraph = solution.GetProjectDependencyGraph();
            foreach (ProjectId projectId in solutionGraph.GetTopologicallySortedProjects())
            {
                Project csProject = solution.GetProject(projectId)!;

                (MemoryStream assemblyStream, Dictionary<string, List<string>> filesToTypeNamesMap) = await CompileProject(csProject);
                if (assemblyStream == null)
                    continue;

                Assembly asm = assemblyContext.LoadFromStream(assemblyStream);
                assemblyStream.Close();

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
            }

            int assembliesCount = assemblyContext.Assemblies.Count();
            if (assembliesCount == 0)
            {
                Logger.Log(LogType.Error, $"Recompile failed! Loaded 0 assemblies for solution at {currentProject.FolderPath}");
                assemblyContext.Unload();
                return false;
            }

            string assembliesNames = string.Join("; ", assemblyContext.Assemblies.Select(x => x.FullName));
            Logger.Log(LogType.Info, $"Recompile succeeded! Loaded {assembliesCount} assemblies for solution at {currentProject.FolderPath}: {assembliesNames}");

            currentAssemblyContext = assemblyContext;
            filesToTypesMap.Clear();
            filesToTypesMap.AddRange(filesToTypes);
            return true;
        }

        private static async Task<(MemoryStream stream, Dictionary<string, List<string>> filesToTypeNamesMap)> CompileProject(Project csProject)
        {
            Compilation compilation = await csProject.GetCompilationAsync();
            if (compilation == null)
            {
                Logger.Log(LogType.Error, $"Compilation failed! Could not precompile project {csProject.Name}. SupportsCompilation = {csProject.SupportsCompilation}");
                return (null, null);
            }

            string inputPath = AssetsRegistry.ContentFolderPath;
            string outputPath = csProject.OutputFilePath;

            var resourceDescriptions = CollectResources(inputPath, outputPath, csProject.DefaultNamespace, csProject.AssemblyName);

            MemoryStream stream = new MemoryStream();
            EmitResult result = compilation.Emit(stream, manifestResources: resourceDescriptions.ToArray());
            stream.Position = 0;

            foreach (Diagnostic diagnostic in result.Diagnostics)
            {
                switch (diagnostic.Severity)
                {
                    case DiagnosticSeverity.Info:
                        Logger.Log(LogType.Info, $"Compilation: {diagnostic.GetMessage()}");
                        break;
                    case DiagnosticSeverity.Warning:
                        Logger.Log(LogType.Warning, $"Compilation: {diagnostic.GetMessage()}");
                        break;
                    case DiagnosticSeverity.Error:
                        Logger.Log(LogType.Error, $"Compilation: {diagnostic.GetMessage()}");
                        break;
                }
            }

            if (!result.Success)
            {
                return (null, null);
            }

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

                filesToTypeNamesMap[filePath].Add(typeSymbol.ToDisplayString(displayFormat));
            }

            return (stream, filesToTypeNamesMap);
        }

        private static List<ResourceDescription> CollectResources(string inputPath, string outputPath, string RootNamespace, string assemblyName)
        {
            outputPath = Path.GetDirectoryName(outputPath);
            List<ResourceDescription> resourceDescriptions = new List<ResourceDescription>();

            string resourcePath = string.Format("{0}\\{1}.g.resources", outputPath, RootNamespace);
            ResourceWriter rsWriter = new ResourceWriter(resourcePath);

            foreach (string file in Directory.GetFiles(outputPath).Where(item => item.EndsWith(".baml")))
            {
                Debug.WriteLine($"FOUND BAML: {file}");
                var fileName = "content/" + Path.GetFileName(file.ToLower());
                var data = File.OpenRead(file);
                rsWriter.AddResource(fileName, data, true);
            }

            rsWriter.Generate();
            rsWriter.Close();
            
            var resourceDescription = new ResourceDescription(
                string.Format("{0}.g.resources", RootNamespace),
                () => File.OpenRead(resourcePath),
                true);
            resourceDescriptions.Add(resourceDescription);

            resourceDescription = new ResourceDescription(
                    string.Format("{0}.{1}.g.resources", assemblyName, RootNamespace),
                    () => File.OpenRead(resourcePath),
                    true);
            resourceDescriptions.Add(resourceDescription);

            return resourceDescriptions;
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

        private static void SanitizeUnloadingContexts()
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

        private static void OnAssemblyContextUnloading(AssemblyLoadContext context)
        {
            string contextName = context.Name;
            WeakReference contextRef = new WeakReference(context);

            unloadingContexts.Add((contextName, contextRef));
        }

        private static void OnFilesChanged(object sender, FileSystemEventArgs e)
        {
            IsCompilationRelevant = false;
        }
    }
}