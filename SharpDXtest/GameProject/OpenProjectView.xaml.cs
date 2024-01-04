using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

            ProjectViewModel project = OpenProjectViewModel.Open(selectedProjectData);
            Window win = Window.GetWindow(this)!;

            if (project != null)
            {
                win.DataContext = project;
                win.DialogResult = true;
            }

            win.Close();
        }
    }
}