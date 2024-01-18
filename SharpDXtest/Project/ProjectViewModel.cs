using System.Collections.Generic;
using System.IO;
using System.Linq;

using Engine;
using Engine.AssetsData;

using static Engine.FileSystemHelper;

namespace Editor
{
    public class ProjectViewModel : ViewModelBase
    {
        public static ProjectViewModel Current { get; private set; }

        public string Name { get; }
        public string FolderPath { get; }

        public BuildSettings BuildSettings { get; private set; }
        public ProjectGraphicsSettings ProjectGraphicsSettings { get; private set; }

        private ProjectViewModel(string name, string folderPath)
        {
            Name = name;
            FolderPath = folderPath;

            Game.Initialize(Name, FolderPath);
        }

        public static bool LoadData(ProjectData projectData)
        {
            Current?.Unload();
            Current = null;

            Current = new ProjectViewModel(projectData.ProjectName, projectData.ProjectFolderPath);

            Current.ProjectGraphicsSettings = new ProjectGraphicsSettings() { GameGraphicsSettings = GameGraphicsSettings.Default() };
            Current.BuildSettings = new BuildSettings();

            return true;
        }

        public void Unload()
        {
            if (filesWatcher != null)
            {
                filesWatcher.EnableRaisingEvents = false;
                filesWatcher.Dispose();
            }
            SaveData();
            Game.Unload();
        }

        public void ApplyProjectSettings()
        {
            SaveData();
            Game.Unload();

            Game.GraphicsSettings = ProjectGraphicsSettings.GameGraphicsSettings;

            Game.Load();
        }

        public void SaveData() { }


        private FileSystemWatcher filesWatcher;

        public void MonitorGameScenes()
        {
            UpdateGameScenes();

            filesWatcher = new FileSystemWatcher();
            filesWatcher.EnableRaisingEvents = false;
            filesWatcher.IncludeSubdirectories = true;
            filesWatcher.Filter = "*.scene";
            filesWatcher.NotifyFilter = NotifyFilters.CreationTime
                                        | NotifyFilters.DirectoryName
                                        | NotifyFilters.FileName;
                                        // | NotifyFilters.LastWrite;

            filesWatcher.Path = AssetsRegistry.ContentFolderPath;
            filesWatcher.Changed += OnSceneFilesChanged;
        }

        public void UpdateGameScenes()
        {
            Dictionary<string, string> scenes = new Dictionary<string, string>();

            IEnumerable<PathInfo> sceneFiles = EnumeratePathInfoEntries(AssetsRegistry.ContentFolderPath, "*.scene", true);
            foreach (PathInfo sceneFile in sceneFiles)
            {
                string scenePath = AssetsRegistry.GetContentAssetPath(sceneFile.FullPath);
                scenes[scenePath] = Path.GetFileNameWithoutExtension(sceneFile.FullPath);
            }

            Game.Scenes = scenes;
            Game.StartingSceneName = scenes.Values.ElementAtOrDefault(BuildSettings.StartingSceneIndex);
        }

        public void SaveCurrentScene()
        {
            if (Scene.CurrentScene is null)
                return;

            if (!AssetsRegistry.TryGetAssetPath(Scene.CurrentScene.Guid, out string path))
            {
                Logger.Log(LogType.Error, "Current scene not found in registry");
                return;
            }

            AssetsRegistry.SaveAsset(path, SceneData.FromScene(Scene.CurrentScene));
        }

        private void OnSceneFilesChanged(object sender, FileSystemEventArgs e)
        {
            filesWatcher.EnableRaisingEvents = false;
            UpdateGameScenes();
            filesWatcher.EnableRaisingEvents = true;
        }
    }

    public class ProjectGraphicsSettings
    {
        public GameGraphicsSettings GameGraphicsSettings { get; set; }
    }

    public class BuildSettings
    {
        public readonly int StartingSceneIndex = 0;
        public readonly Dictionary<string, string> Scenes = new Dictionary<string, string>();
    }
}