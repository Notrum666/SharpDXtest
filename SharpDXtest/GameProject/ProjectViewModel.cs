using System;

using Engine;

namespace Editor
{
    public class ProjectViewModel : ViewModelBase
    {
        public static ProjectViewModel Current { get; private set; }

        public string Name { get; }
        public string FolderPath { get; }

        public ProjectGraphicsSettings GraphicsSettings { get; }
        public ProjectScenesSettings ScenesSettings { get; }

        private ProjectViewModel(string name, string folderPath)
        {
            Name = name;
            FolderPath = folderPath;

            GraphicsSettings = ProjectGraphicsSettings.Default();
            ScenesSettings = new ProjectScenesSettings();
        }

        public static bool Load(ProjectData projectData)
        {
            Current?.Save();
            Current?.Unload();
            Current = null;

            Current = new ProjectViewModel(projectData.ProjectName, projectData.ProjectFolderPath);

            string projectFolderPath = Current.FolderPath;
            AssetsManager.InitializeInFolder(projectFolderPath);
            AssetsRegistry.InitializeInFolder(projectFolderPath);
            SceneManager.UpdateScenesList(Current.ScenesSettings.Scenes);

            return true;
        }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public void Unload()
        {
            GraphicsSettings.Unload();
        }
    }
}