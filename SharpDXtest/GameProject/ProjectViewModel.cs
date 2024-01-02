using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Editor.GameProject
{
    [DataContract(Name = "Game")]
    class ProjectViewModel : ViewModelBase
    {
        public static string Extension { get; } = ".sharpdx";
        [DataMember]
        public string Name { get; private set; }
        [DataMember]
        public string Path { get; private set; }

        public string FullPath => $"{Path}{Name}{Extension}";

        [DataMember(Name = "Scenes")]
        private ObservableCollection<SceneViewModel> scenes = new ObservableCollection<SceneViewModel>();
        public ReadOnlyObservableCollection<SceneViewModel> Scenes { get; }

        public ProjectViewModel(string name, string path)
        {
            Name = name;
            Path = path;

            scenes.Add(new SceneViewModel(this, "Default Scene"));
        }
    }
}
