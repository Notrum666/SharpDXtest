using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using Engine;

namespace Editor.GameProject
{
    public class CreateProjectViewModel : ViewModelBase
    {
        private const string TemplatesFolderName = "ProjectTemplates";

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

        private string parentFolderPath = Path.Combine($"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}", "SharpDxProjects");
        public string ParentFolderPath
        {
            get => parentFolderPath;
            set
            {
                if (value != parentFolderPath)
                {
                    parentFolderPath = value;
                    ValidateProjectPath();
                    OnPropertyChanged();
                }
            }
        }

        private bool isValid;
        public bool IsValid
        {
            get => isValid;
            private set
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
            private set
            {
                if (value != errorMsg)
                {
                    errorMsg = value;
                    OnPropertyChanged();
                }
            }
        }

        private readonly ObservableCollection<ProjectTemplate> projectTemplates = new ObservableCollection<ProjectTemplate>();
        public ReadOnlyObservableCollection<ProjectTemplate> ProjectTemplates { get; }

        public CreateProjectViewModel()
        {
            ProjectTemplates = new ReadOnlyObservableCollection<ProjectTemplate>(projectTemplates);

            string templatesFolderPath = Path.Combine(EditorWindow.ResourcesFolderPath, TemplatesFolderName);
            string[] templateFiles = Directory.GetFiles(templatesFolderPath, "template.asset", SearchOption.AllDirectories);
            foreach (string file in templateFiles)
            {
                //ProjectTemplate template = new ProjectTemplate()
                //{
                //    ProjectType = "Empty Project",
                //    ProjectFile = "project.sharpdx",
                //    CsprojFile = "csproj",
                //    SolutionFile = "sln",
                //};
                //YamlManager.SaveToFile(file, template);

                ProjectTemplate template = YamlManager.LoadFromFile<ProjectTemplate>(file);

                template.TemplateFolderPath = Path.GetDirectoryName(file);
                template.Icon = File.ReadAllBytes(template.IconPath);
                template.Screenshot = File.ReadAllBytes(template.ScreenshotPath);

                projectTemplates.Add(template);
            }

            ValidateProjectPath();
        }

        public ProjectData CreateProject(ProjectTemplate template)
        {
            ValidateProjectPath();
            if (!IsValid)
                return null;

            return ProjectsManager.CreateProject(ParentFolderPath, ProjectName, template);
        }

        private void ValidateProjectPath()
        {
            string projectFolderPath = Path.Combine(ParentFolderPath, ProjectName);

            IsValid = false;

            if (string.IsNullOrEmpty(ProjectName.Trim()))
            {
                ErrorMsg = "Type in a project name.";
            }
            else if (ProjectName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                ErrorMsg = "Invalid character(s) used in project name.";
            }
            else if (string.IsNullOrEmpty(ParentFolderPath.Trim()))
            {
                ErrorMsg = "Select a valid project folder.";
            }
            else if (ParentFolderPath.IndexOfAny(Path.GetInvalidPathChars()) != -1)
            {
                ErrorMsg = "Invalid character(s) used in project path.";
            }
            else if (Directory.Exists(projectFolderPath) && Directory.EnumerateFileSystemEntries(projectFolderPath).Any())
            {
                ErrorMsg = "Selected project folder already exists and is not empty.";
            }
            else
            {
                ErrorMsg = string.Empty;
                IsValid = true;
            }
        }
    }
}