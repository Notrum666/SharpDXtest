using System.Windows;
using System.Windows.Controls;

namespace Editor.GameProject
{
    /// <summary>
    /// Interaction logic for CreateProjectView.xaml
    /// </summary>
    public partial class CreateProjectView : UserControl
    {
        public CreateProjectView()
        {
            InitializeComponent();

            DataContext = new CreateProjectViewModel();
        }

        private void CreateButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is not CreateProjectViewModel createProjectViewModel)
                return;

            if (TemplateListBox.SelectedItem is not ProjectTemplate selectedTemplate)
                return;

            string projectPath = createProjectViewModel.CreateProject(selectedTemplate);
            Window win = Window.GetWindow(this)!;

            if (!string.IsNullOrEmpty(projectPath))
            {
                ProjectData projectData = new ProjectData() { ProjectName = createProjectViewModel.ProjectName, ProjectPath = projectPath };
                ProjectViewModel project = OpenProjectViewModel.Open(projectData);

                if (project != null)
                {
                    win.DataContext = project;
                    win.DialogResult = true;
                }
            }

            win.Close();
        }
    }
}