using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Engine;

using YamlDotNet.Serialization.Callbacks;

using static Engine.FileSystemHelper;

namespace Editor
{
    public static class ProjectsManager
    {
        public const string Extension = ".sharpdx";
        public const string MetaFolderName = ".SharpDX";
        private const string BaseAssetsFolderName = "BaseAssets";

        public static ReadOnlyObservableCollection<ProjectData> Projects { get; private set; }

        private static ProjectsDatabase projectsDatabase;
        public static void InitializeInFolder(string dataFolderPath)
        {
            Logger.Log(LogType.Info, $"Initialize ProjectsManager in folder = {dataFolderPath}");
            Directory.CreateDirectory(dataFolderPath);
            projectsDatabase = ProjectsDatabase.Load(dataFolderPath);
            Projects = new ReadOnlyObservableCollection<ProjectData>(projectsDatabase.ProjectsList);
        }

        public static ProjectData CreateProject(string parentFolderPath, string projectName, ProjectTemplate template)
        {
            string projectFolderPath = Path.Combine(parentFolderPath, projectName);
            ProjectData projectData = new ProjectData(projectName, projectFolderPath);

            Directory.CreateDirectory(projectFolderPath);
            foreach (string defaultFolder in ProjectTemplate.DefaultFolders)
            {
                string defaultFolderPath = Path.Combine(projectFolderPath, defaultFolder);
                Directory.CreateDirectory(defaultFolderPath);
            }

            //Meta
            DirectoryInfo dirInfo = new DirectoryInfo(projectData.MetaFolderPath);
            dirInfo.Attributes |= FileAttributes.Hidden;
            File.Copy(template.IconPath, projectData.IconPath);
            File.Copy(template.ScreenshotPath, projectData.ScreenshotPath);

            //Project file
            string templateProjectData = File.ReadAllText(template.ProjectTemplatePath);
            templateProjectData = string.Format(templateProjectData, projectName, projectFolderPath);
            File.WriteAllText(projectData.ProjectFilePath, templateProjectData);

            //Solution
            string projectGuid = Guid.NewGuid().ToString("B");
            string assemblyName = Regex.Replace(projectName, "[^a-zA-Z0-9]", "_");

            string templateCsprojData = File.ReadAllText(template.CsprojTemplatePath);
            templateCsprojData = string.Format(templateCsprojData, projectGuid, assemblyName, $"$({EditorLayer.EditorPathVarName})");
            string csprojFilePath = Path.Combine(projectData.ProjectFolderPath, $"{assemblyName}.csproj");
            File.WriteAllText(csprojFilePath, templateCsprojData);

            string templateSolutionData = File.ReadAllText(template.SolutionTemplatePath);
            templateSolutionData = string.Format(templateSolutionData, projectGuid, assemblyName);
            string solutionFilePath = Path.Combine(projectData.ProjectFolderPath, $"{assemblyName}.sln");
            File.WriteAllText(solutionFilePath, templateSolutionData);

            //Content
            string baseAssetsPath = Path.Combine(EditorLayer.Current.ResourcesFolderPath, BaseAssetsFolderName);
            string projectContentPath = Path.Combine(projectData.ProjectFolderPath, AssetsRegistry.ContentFolderName);
            CopyFolder(baseAssetsPath, Path.Combine(projectContentPath, BaseAssetsFolderName));

            projectsDatabase.AddProjectData(projectData);
            return projectData;
        }

        internal static bool TryAddProject(string projectFile, out ProjectData newProjectData)
        {
            if (!Path.Exists(projectFile))
            {
                Logger.Log(LogType.Warning, $"Tried to add non-existent project at path \"{projectFile}\"");
                newProjectData = null;
                return false;
            }

            if (Path.GetExtension(projectFile) != Extension)
            {
                Logger.Log(LogType.Warning, $"Tried to add project with wrong extension at path \"{projectFile}\"");
                newProjectData = null;
                return false;
            }

            string projectName = Path.GetFileNameWithoutExtension(projectFile);
            string projectFolder = Path.GetDirectoryName(projectFile);

            newProjectData = new ProjectData(projectName, projectFolder);
            return projectsDatabase.AddProjectData(newProjectData);
        }

        private static bool CopyFolder(string folderPath, string newFolderPath)
        {
            if (!Path.Exists(folderPath))
                return false;

            Directory.CreateDirectory(newFolderPath);

            foreach (PathInfo pathInfo in EnumeratePathInfoEntries(folderPath, "*", true))
            {
                if (!pathInfo.IsDirectory)
                    continue;
                string relativePath = Path.GetRelativePath(folderPath, pathInfo.FullPath);
                Directory.CreateDirectory(Path.Combine(newFolderPath, relativePath));
            }

            foreach (PathInfo pathInfo in EnumeratePathInfoEntries(folderPath, "*", true))
            {
                if (pathInfo.IsDirectory)
                    continue;
                string relativePath = Path.GetRelativePath(folderPath, pathInfo.FullPath);
                File.Copy(pathInfo.FullPath, Path.Combine(newFolderPath, relativePath), true);
            }

            return true;
        }
    }

    public class ProjectsDatabase
    {
        private const int LatestVersion = 0;
        private const string FileName = "Projects.db";

        private string filePath;

        [SerializedField]
        private readonly int version;
        [SerializedField]
        public readonly ObservableCollection<ProjectData> ProjectsList = new ObservableCollection<ProjectData>();

        public ProjectsDatabase()
        {
            version = LatestVersion;
        }

        internal static ProjectsDatabase Load(string parentFolderPath)
        {
            string databaseFilePath = Path.Combine(parentFolderPath, FileName);

            ProjectsDatabase projectsDatabase = YamlManager.LoadFromFile<ProjectsDatabase>(databaseFilePath);

            if (projectsDatabase == null || projectsDatabase.version != LatestVersion)
                projectsDatabase = new ProjectsDatabase();

            projectsDatabase.filePath = databaseFilePath;
            projectsDatabase.SanitizeProjectsList();
            return projectsDatabase;
        }

        private void Save()
        {
            if (ProjectsList.Count != 0)
                YamlManager.SaveToFile(filePath, this);
        }

        internal bool AddProjectData(ProjectData newProjectData)
        {
            string projectFolderPath = newProjectData.ProjectFolderPath;
            if (ProjectsList.Any(x => x.ProjectFolderPath == projectFolderPath))
                return false;

            newProjectData.LoadMetaImages();

            ProjectsList.Add(newProjectData);
            SanitizeProjectsList();
            return true;
        }

        private void SanitizeProjectsList()
        {
            List<ProjectData> projects = ProjectsList.ToList();
            foreach (ProjectData project in ProjectsList)
            {
                if (!File.Exists(project.ProjectFilePath))
                    projects.Remove(project);
            }

            ProjectsList.Clear();
            foreach (ProjectData project in projects.OrderBy(x => x.ProjectName))
            {
                ProjectsList.Add(project);
            }
            Save();
        }
    }

    public class ProjectData
    {
        [SerializedField]
        private readonly string projectName;
        [SerializedField]
        private readonly string projectFolderPath;

        public string ProjectName => projectName;

        /// <summary>Usually equals ParentFolderPath + ProjectName</summary>
        public string ProjectFolderPath => projectFolderPath;

        public string ProjectFilePath => Path.Combine(ProjectFolderPath, $"{ProjectName}{ProjectsManager.Extension}");
        public string MetaFolderPath => Path.Combine(ProjectFolderPath, ProjectsManager.MetaFolderName);
        public string IconPath => Path.Combine(MetaFolderPath, "Icon.png");
        public string ScreenshotPath => Path.Combine(MetaFolderPath, "Screenshot.png");

        public byte[] Icon { get; set; }
        public byte[] Screenshot { get; set; }

        public ProjectData() { }

        public ProjectData(string name, string folderPath)
        {
            projectName = name;
            projectFolderPath = folderPath;
        }

        [OnDeserialized]
        public void LoadMetaImages()
        {
            if (File.Exists(IconPath))
                Icon = File.ReadAllBytes(IconPath);
            if (File.Exists(ScreenshotPath))
                Screenshot = File.ReadAllBytes(ScreenshotPath);
        }
    }

    public class ProjectTemplate
    {
        public static readonly IReadOnlyList<string> DefaultFolders = new List<string>()
        {
            ProjectsManager.MetaFolderName,
            AssetsManager.ArtifactFolderName,
            AssetsRegistry.ContentFolderName,
            "Temp"
        };

        [SerializedField("ProjectType")]
        private readonly string projectType;
        public readonly string ProjectFile;
        public readonly string CsprojFile;
        public readonly string SolutionFile;

        public string TemplateFolderPath { get; set; }

        public string ProjectTemplatePath => Path.Combine(TemplateFolderPath, ProjectFile);
        public string CsprojTemplatePath => Path.Combine(TemplateFolderPath, CsprojFile);
        public string SolutionTemplatePath => Path.Combine(TemplateFolderPath, SolutionFile);

        public string IconPath => Path.Combine(TemplateFolderPath, "Icon.png");
        public string ScreenshotPath => Path.Combine(TemplateFolderPath, "Screenshot.png");

        public string ProjectType { get => projectType; init => projectType = value; }
        public byte[] Icon { get; set; }
        public byte[] Screenshot { get; set; }
    }
}