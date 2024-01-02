using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Editor.GameProject
{
    [DataContract]
    class SceneViewModel : ViewModelBase
    {
        private string name;
        [DataMember]
        public string Name
        {
            get => name;
            set
            {
                if (value != name)
                {
                    name = value;
                    OnPropertyChanged();
                }
            }
        }
        [DataMember]
        public ProjectViewModel Project { get; private set; }

        public SceneViewModel(ProjectViewModel project, string name)
        {
            Debug.Assert(project != null);
            Project = project;
            Name = name;
        }
    }
}
