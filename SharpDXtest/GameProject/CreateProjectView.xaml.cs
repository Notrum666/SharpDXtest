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
        }

        private void CreateButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is not CreateProjectViewModel createProjectViewModel)
                return;

            if (TemplateListBox.SelectedItem is not ProjectTemplate selectedTemplate)
                return;

            ProjectData projectData = createProjectViewModel.CreateProject(selectedTemplate);
            if (projectData == null)
                return;

            Window win = Window.GetWindow(this)!;
            win.DialogResult = ProjectViewModel.Load(projectData);
            win.Close();
        }
    }
}