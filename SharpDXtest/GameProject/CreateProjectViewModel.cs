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
    public class ProjectTemplate
    {
        [DataMember]
        public string ProjectType { get; set; }
        [DataMember]
        public string ProjectFile { get; set; }
        [DataMember]
        public List<string> Folders { get; set; }

        public string IconFilePath { get; set; }
        public byte[] Icon { get; set; }

        public string ScreenshotFilePath { get; set; }
        public byte[] Screenshot { get; set; }

        public string ProjectFilePath { get; set; }
    }

    internal class CreateProjectViewModel : ViewModelBase
    {
        //TODO: get the path from the installation location
        private const string TemplatePath = @"..\..\..\..\..\SharpDxTest\ProjectTemplates";
        private const string ProjectRootFolder = "SharpDxProject";

        private string projectName = "NewProject";
        public string ProjectName
        {
            get => projectName;
            set
            {
                if (value != projectName)
                {
                    projectName = value;
                    OnPropertyChanged();
                }
            }
        }

        //private string path = $@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\SharpDxProject\";
        private string projectPath = Path.Combine($"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}", ProjectRootFolder);
        public string ProjectPath
        {
            get => projectPath;
            set
            {
                if (value != projectPath)
                {
                    projectPath = value;
                    OnPropertyChanged();
                }
            }
        }

        private ObservableCollection<ProjectTemplate> projectTemplates = new ObservableCollection<ProjectTemplate>();
        public ReadOnlyObservableCollection<ProjectTemplate> ProjectTemplates { get; }

        public CreateProjectViewModel()
        {
            ProjectTemplates = new ReadOnlyObservableCollection<ProjectTemplate>(projectTemplates);

            try
            {
                string[] templateFiles = Directory.GetFiles(TemplatePath, "template.xml", SearchOption.AllDirectories);
                Debug.Assert(templateFiles.Any());
                foreach (string file in templateFiles)
                {
                    //ProjectTemplate template = new ProjectTemplate()
                    //{
                    //    ProjectType = "Empty Project",
                    //    ProjectFile = "project.sharpdx",
                    //    Folders = new List<string>() { ".SharpDX", "Content", "GameCode" }
                    //};

                    //ContractSerializer.ToFile(template, file);

                    ProjectTemplate template = ContractSerializer.FromFile<ProjectTemplate>(file);

                    template.IconFilePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file), "Icon.png"));
                    template.Icon = File.ReadAllBytes(template.IconFilePath);

                    template.ScreenshotFilePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file), "Screenshot.png"));
                    template.Screenshot = File.ReadAllBytes(template.ScreenshotFilePath);

                    template.ProjectFilePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file), template.ProjectFile));

                    projectTemplates.Add(template);
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
