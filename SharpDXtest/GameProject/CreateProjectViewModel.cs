using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private const string TemplatePath = @"..\..\..\..\..\SharpDxTest\ProjectTemplates\";
        private const string ProjectRootFolder = "SharpDxProjects";

        private string projectName = "NewProject";
        public string ProjectName
        {
            get => projectName;
            set
            {
                if (value != projectName)
                {
                    projectName = value;
                    ValidateProjectPath();
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
                    ValidateProjectPath();
                    OnPropertyChanged();
                }
            }
        }

        private bool isValid;
        public bool IsValid
        {
            get => isValid;
            set
            {
                if (value != isValid)
                {
                    isValid = value;
                    OnPropertyChanged();
                }
            }
        }

        private string errorMsg;
        public string ErrorMsg
        {
            get => errorMsg;
            set
            {
                if (value != errorMsg)
                {
                    errorMsg = value;
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

                ValidateProjectPath();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public string CreateProject(ProjectTemplate template)
        {
            ValidateProjectPath();
            if (!IsValid)
                return string.Empty;

            if (!Path.EndsInDirectorySeparator(ProjectPath))
                ProjectPath += @"\";
            string path = $@"{ProjectPath}{ProjectName}\";

            try
            {
                Directory.CreateDirectory(path);
                foreach (string folder in template.Folders)
                {
                    //string folderPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path), folder));
                    string folderPath = Path.Combine(path, folder);
                    Directory.CreateDirectory(folderPath);
                }
                DirectoryInfo dirInfo = new DirectoryInfo(path + @".SharpDX\");
                dirInfo.Attributes |= FileAttributes.Hidden;
                File.Copy(template.IconFilePath, Path.GetFullPath(Path.Combine(dirInfo.FullName, "Icon.png")));
                File.Copy(template.ScreenshotFilePath, Path.GetFullPath(Path.Combine(dirInfo.FullName, "Screenshot.png")));

                var projectXml = File.ReadAllText(template.ProjectFilePath);
                projectXml = string.Format(projectXml, ProjectName, ProjectPath);

                string projectPath = Path.GetFullPath(Path.Combine(path, $"{ProjectName}{ProjectViewModel.Extension}"));
                File.WriteAllText(projectPath, projectXml);

                return path;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                //TODO: log error
                return string.Empty;
            }
        }

        private bool ValidateProjectPath()
        {
            string path = ProjectPath;

            if (!Path.EndsInDirectorySeparator(path))
                path += @"\";

            path += $@"{ProjectName}\";

            IsValid = false;
            if (string.IsNullOrEmpty(ProjectName.Trim()))
            {
                ErrorMsg = "Type in a project name.";
            }
            else if (ProjectName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                ErrorMsg = "Invalid character(s) used in project name.";
            }
            else if (string.IsNullOrEmpty(ProjectPath.Trim()))
            {
                ErrorMsg = "Select a valid project folder.";
            }
            else if (ProjectPath.IndexOfAny(Path.GetInvalidPathChars()) != -1)
            {
                ErrorMsg = "Invalid character(s) used in project path.";
            }
            else if(Directory.Exists(path) && Directory.EnumerateFileSystemEntries(path).Any())
            {
                ErrorMsg = "Selected project folder already exists and is not empty.";
            }
            else
            {
                ErrorMsg = string.Empty;
                IsValid = true;
            }

            return IsValid;
        }
    }
}
