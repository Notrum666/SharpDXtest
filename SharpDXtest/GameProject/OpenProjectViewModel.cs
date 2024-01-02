using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Editor.GameProject
{
    [DataContract]
    public class ProjectData
    {
        [DataMember]
        public string ProjectName { get; set; }
        [DataMember]
        public string ProjectPath { get; set; }
        [DataMember]
        public DateTime Date { get; set; }

        public string FullPath => $"{ProjectPath}{ProjectName}{ProjectViewModel.Extension}";
        public byte[] Icon { get; set; }
        public byte[] Screenshot { get; set; }
    }

    [DataContract]
    public class ProjectDataList
    {
        [DataMember]
        public List<ProjectData> Projects { get; set; }
    }

    class OpenProjectViewModel : ViewModelBase
    {
        private static readonly string applicationDataPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\SharpDxEditor\";
        private static readonly string projectDataPath;

        private static readonly ObservableCollection<ProjectData> projects = new ObservableCollection<ProjectData>();
        public static ReadOnlyObservableCollection<ProjectData> Projects { get; }

        static OpenProjectViewModel()
        {
            try
            {
                Directory.CreateDirectory(applicationDataPath);
                projectDataPath = $@"{applicationDataPath}ProjectData.xml";
                ReadProjectData();
            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
                // TODO: log errors
            }
        }

        public static ProjectViewModel Open(ProjectData data)
        {
            ReadProjectData();

            var project = projects.FirstOrDefault(x => x.FullPath == data.FullPath);
            if (project != null)
            {
                project.Date = DateTime.Now;
            }
            else
            {
                project = data;
                project.Date = DateTime.Now;
                projects.Add(project);
            }

            WriteProjectData();
            return ProjectViewModel.Load(project.FullPath);
        }

        private static void WriteProjectData()
        {
            var orderedProjects = projects.OrderBy(x => x.Date).ToList();
            ContractSerializer.ToFile(new ProjectDataList() { Projects = orderedProjects }, projectDataPath);
        }

        private static void ReadProjectData()
        {
            if (File.Exists(projectDataPath))
            {
                var orderedProjects = ContractSerializer.FromFile<ProjectDataList>(projectDataPath).Projects.OrderByDescending(x => x.Date);
                projects.Clear();

                foreach (ProjectData project in orderedProjects)
                {
                    if (File.Exists(project.FullPath))
                    {
                        project.Icon = File.ReadAllBytes($@"{project.ProjectPath}\.SharpDX\Icon.png");
                        project.Screenshot = File.ReadAllBytes($@"{project.ProjectPath}\.SharpDX\Screenshot.png");
                    }
                }
            }
        }
    }
}
