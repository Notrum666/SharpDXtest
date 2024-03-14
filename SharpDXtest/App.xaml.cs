using System;
using System.Globalization;
using System.Threading;
using System.Windows;

using Editor;

using LinearAlgebra;

namespace SharpDXtest
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static bool IsActive { get; private set; }

        void App_Activated(object sender, EventArgs e)
        {
            // Application activated
            IsActive = true;
        }

        void App_Deactivated(object sender, EventArgs e)
        {
            // Application deactivated
            IsActive = false;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-us");

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Dialog window must be created after editor layer is initialized, otherwise
            // it doesn't know about Projects property value change
            if (EditorLayer.Current != null && new ProjectBrowserDialogWindow().ShowDialog() == false || ProjectViewModel.Current == null)
            {
                Shutdown();
                return;
            }

            // re-copy base assets to current project folder
            ProjectsManager.RecopyBaseAssets();

            EditorWindow editorWindow = new EditorWindow();
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            MainWindow = editorWindow;
            editorWindow.Show();
        }
    }
}