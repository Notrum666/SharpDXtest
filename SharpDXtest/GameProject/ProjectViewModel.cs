using System;
using System.Collections.ObjectModel;

using Engine;

namespace Editor.GameProject
{
    class ProjectViewModel : ViewModelBase
    {
        public static ProjectViewModel Current { get; private set; }

        public string Name { get; }
        public string FolderPath { get; }
        public ReadOnlyObservableCollection<string> Scenes { get; }
        public string ActiveScene
        {
            get => activeScene;
            set
            {
                if (value != activeScene)
                {
                    activeScene = value;
                    OnPropertyChanged();
                }
            }
        }

        private string activeScene;
        private readonly ObservableCollection<string> scenes = new ObservableCollection<string>();

        private ProjectViewModel(string name, string folderPath)
        {
            Name = name;
            FolderPath = folderPath;

            Scenes = new ReadOnlyObservableCollection<string>(scenes);
        }

        public static bool Load(ProjectData projectData)
        {
            ProjectViewModel project = new ProjectViewModel(projectData.ProjectName, projectData.ProjectFolderPath);

            // var mainPath = Directory.GetCurrentDirectory();
            // var projectFolderPath = Directory.GetParent(mainPath)?.Parent?.Parent?.Parent?.Parent?.FullName;
            string projectFolderPath = project.FolderPath;
            AssetsManager.InitializeInFolder(projectFolderPath);
            AssetsRegistry.InitializeInFolder(projectFolderPath);

            Current?.Save();
            Current?.Unload();
            Current = project;
            return true;
        }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public void Unload()
        {
            throw new NotImplementedException();
        }
    }
}