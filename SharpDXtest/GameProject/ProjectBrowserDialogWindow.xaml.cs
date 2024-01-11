using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

using Engine;

using Microsoft.Win32;

namespace Editor
{
    /// <summary>
    /// Interaction logic for ProjectBrowserDialog.xaml
    /// </summary>
    public partial class ProjectBrowserDialogWindow : CustomWindowBase, INotifyPropertyChanged
    {
        private const string TemplatesFolderName = "ProjectTemplates";

        // because the Model is the window itself (mainly it's DialogueResult) - the ViewModel is not separated
        private RelayCommand openProjectCommand;
        public RelayCommand OpenProjectCommand => openProjectCommand ?? 
            (openProjectCommand = new RelayCommand(obj =>
            {
                IsEnabled = false;
                DialogResult = ProjectViewModel.Load((ProjectData)obj);
                Close();
            },
                obj => obj is ProjectData));
        private RelayCommand browseCommand;
        public RelayCommand BrowseCommand => browseCommand ??
            (browseCommand = new RelayCommand(obj =>
            {
                string projectFilter = $"*{ProjectsManager.Extension}";
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    Filter = $"Project files ({projectFilter})|{projectFilter}",
                    Multiselect = false,
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    string projectFilePath = openFileDialog.FileName;
                    if (ProjectsManager.TryAddProject(projectFilePath, out ProjectData projectData))
                        ProjectsListBox.SelectedItem = projectData;
                }
            }));
        private RelayCommand browseFolderCommand;
        public RelayCommand BrowseFolderCommand => browseFolderCommand ??
            (browseFolderCommand = new RelayCommand(obj =>
            {
                FolderPicker folderPicker = new FolderPicker
                {
                    InputPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    Multiselect = false,
                    Title = "Select Folder",
                    OkButtonLabel = "Select Folder",
                };
                if (folderPicker.ShowDialog() == true)
                {
                    ParentFolderPath = folderPicker.ResultPath;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ParentFolderPath)));
                }
            }));
        private RelayCommand createProjectCommand;
        public RelayCommand CreateProjectCommand => createProjectCommand ??
            (createProjectCommand = new RelayCommand(obj =>
            {
                ProjectData data = ProjectsManager.CreateProject(ParentFolderPath, ProjectName, (ProjectTemplate)obj);
                if (data is null)
                {
                    DialogResult = false;
                    Close();
                }

                IsEnabled = false;
                DialogResult = ProjectViewModel.Load(data);
                Close();
            },
                obj => obj is ProjectTemplate));

        private readonly ObservableCollection<ProjectTemplate> projectTemplates = new ObservableCollection<ProjectTemplate>();
        public ReadOnlyObservableCollection<ProjectTemplate> ProjectTemplates { get; }

        public string ProjectName { get; set; } = "NewProject";
        public string ParentFolderPath { get; set; } = Path.Combine($"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}", "SharpDxProjects");

        public ProjectBrowserDialogWindow()
        {
            InitializeComponent();

            DataContext = this;

            ProjectTemplates = new ReadOnlyObservableCollection<ProjectTemplate>(projectTemplates);

            string templatesFolderPath = Path.Combine(EditorLayer.Current.ResourcesFolderPath, TemplatesFolderName);
            string[] templateFiles = Directory.GetFiles(templatesFolderPath, "template.asset", SearchOption.AllDirectories);
            foreach (string file in templateFiles)
            {
                ProjectTemplate template = YamlManager.LoadFromFile<ProjectTemplate>(file);

                template.TemplateFolderPath = Path.GetDirectoryName(file);
                template.Icon = File.ReadAllBytes(template.IconPath);
                template.Screenshot = File.ReadAllBytes(template.ScreenshotPath);

                projectTemplates.Add(template);
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void ListBox_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ((ListBox)sender).SelectedItem = null;
        }

        private void ListBoxItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenProjectCommand.Execute(((ListBoxItem)sender).DataContext);
        }
    }
}