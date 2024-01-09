using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using Editor.AssetsImport;
using Editor.GameProject;

using Engine;
using Engine.BaseAssets.Components;

using LinearAlgebra;

using SharpDXtest.Assets.Components;

namespace Editor
{
    public partial class EditorWindow : EditorWindowBase
    {
        public const string EditorPathVarName = "EnvVar_SharpDxEditor";
        private const string DataFolderName = "SharpDxEditor";
        private const string ResourcesFolderName = "Resources";

        public static string DataFolderPath { get; private set; }
        public static string EditorFolderPath { get; private set; }
        public static string ResourcesFolderPath { get; private set; }

        private RelayCommand spawnFlyingControl;

        public RelayCommand SpawnFlyingControl => spawnFlyingControl ?? (spawnFlyingControl = new RelayCommand(
            obj =>
            {
                FlyingControl flyingControl = new FlyingControl();
                FrameworkElement item = (FrameworkElement)Activator.CreateInstance((Type)obj);
                flyingControl.Items.Add(item);
                EditorDockingManager.AddFlyingControl(flyingControl);
            },
            obj => obj is Type && (obj as Type).IsSubclassOf(typeof(FrameworkElement))
        ));

        private RelayCommand recompileCommand;

        public RelayCommand RecompileCommand => recompileCommand ??= new RelayCommand(
            _ => { ScriptManager.Recompile(); },
            _ => true
        );

        public EditorWindow()
        {
            DataFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DataFolderName);
            EditorFolderPath = AppDomain.CurrentDomain.BaseDirectory;
            Environment.SetEnvironmentVariable(EditorPathVarName, EditorFolderPath, EnvironmentVariableTarget.User);

            ResourcesFolderPath = Path.Combine(EditorFolderPath, ResourcesFolderName);

            InitializeComponent();

            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-us");

            DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ProjectsManager.InitializeInFolder(DataFolderPath);

            OpenProjectBrowserDialog();
        }

        private void EditorWindowInst_Closing(object sender, CancelEventArgs e)
        {
            if (EngineCore.IsAlive)
                EngineCore.Stop();

            ProjectViewModel.Current?.Unload();
        }

        private void EditorWindowInst_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
        }

        private void OpenProjectBrowserDialog()
        {
            ProjectBrowserDialogWindow projectBrowser = new ProjectBrowserDialogWindow();
            if (projectBrowser.ShowDialog() == false || ProjectViewModel.Current == null)
                Application.Current.Shutdown();
            else
            {
                //SceneManager.Load(ProjectViewModel.Current.ActiveScene)
                EngineCore.Init(EditorLayer.Current);
                EngineCore.IsPaused = true;
                EngineCore.Run();

                ProjectViewModel.Current.GraphicsSettings.Load();
                SceneManager.LoadSceneByName(ProjectViewModel.Current.ScenesSettings.StartingSceneName);
            }
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}