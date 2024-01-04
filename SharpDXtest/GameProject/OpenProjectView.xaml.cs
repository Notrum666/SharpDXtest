using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.Win32;

namespace Editor.GameProject
{
    /// <summary>
    /// Interaction logic for OpenProjectView.xaml
    /// </summary>
    public partial class OpenProjectView : UserControl
    {
        public OpenProjectView()
        {
            InitializeComponent();
        }

        private void AddProjectButton_OnClick(object sender, RoutedEventArgs e)
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
        }

        private void OpenButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenProject();
        }

        private void ProjectsListBox_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenProject();
        }

        private void OpenProject()
        {
            if (ProjectsListBox.SelectedItem is not ProjectData selectedProjectData)
                return;

            Window win = Window.GetWindow(this)!;
            win.DialogResult = ProjectViewModel.Load(selectedProjectData);
            win.Close();
        }
    }
}