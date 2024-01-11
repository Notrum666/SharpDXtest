using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

using Engine;

namespace Editor
{
    public partial class EditorWindow : CustomWindowBase
    {
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

        public EditorWindow()
        {
            InitializeComponent();

            DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // should be not null because this windows opens only after successfull project selection,
            // but kept this check just in case of some weird shit
            if (ProjectViewModel.Current == null)
                throw new Exception("ProjectViewModel is null on editor window load.");

            EngineCore.Init(EditorLayer.Current);
            EngineCore.IsPaused = true;
            EngineCore.Run();

            ProjectViewModel.Current.GraphicsSettings.Load();
            SceneManager.LoadSceneByName(ProjectViewModel.Current.ScenesSettings.StartingSceneName);
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
    }
}