using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Editor.GameProject
{
    [DataContract(Name = "Game")]
    class ProjectViewModel : ViewModelBase
    {
        public static string Extension { get; } = ".sharpdx";

        public static ProjectViewModel Current => Application.Current.MainWindow.DataContext as ProjectViewModel;

        [DataMember]
        public string Name { get; private set; } = "New Project";
        [DataMember]
        public string Path { get; private set; }

        public string FullPath => $"{Path}{Name}{Extension}";

        [DataMember(Name = "Scenes")]
        private ObservableCollection<SceneViewModel> scenes = new ObservableCollection<SceneViewModel>();
        public ReadOnlyObservableCollection<SceneViewModel> Scenes { get; private set; }

        private SceneViewModel activeScene;
        public SceneViewModel ActiveScene
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

        public ProjectViewModel(string name, string path)
        {
            Name = name;
            Path = path;

            OnDeserialized(new StreamingContext());
        }

        public static ProjectViewModel Load(string file)
        {
            Debug.Assert(File.Exists(file));
            return ContractSerializer.FromFile<ProjectViewModel>(file);
        }

        public static void Save(ProjectViewModel project)
        {
            ContractSerializer.ToFile(project, project.FullPath);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (scenes != null)
            {
                Scenes = new ReadOnlyObservableCollection<SceneViewModel>(scenes);
                OnPropertyChanged(nameof(Scenes));
            }
            ActiveScene = Scenes.FirstOrDefault(x => x.IsActive);
        }

        public void Unload()
        {

        }
    }
}
