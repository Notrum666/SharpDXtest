using System.Collections.Generic;
using System.IO;
using System.Linq;

using Engine;

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

        public void UpdateGameScenes()
        {
            Dictionary<string, string> scenes = new Dictionary<string, string>();

            IEnumerable<PathInfo> sceneFiles = EnumeratePathInfoEntries(AssetsRegistry.ContentFolderPath, "*.scene", true);
            foreach (PathInfo sceneFile in sceneFiles)
            {
                scenes[sceneFile.FullPath] = Path.GetFileNameWithoutExtension(sceneFile.FullPath);
            }

            Game.Scenes = scenes;
            Game.StartingSceneName = scenes.Values.ElementAtOrDefault(BuildSettings.StartingSceneIndex);
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