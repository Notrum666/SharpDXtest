using System;
using System.Windows;
using System.Windows.Input;
using System.Threading;
using System.Globalization;
using System.Windows.Interop;
using Engine;

namespace Editor
{
    public partial class EditorWindow : EditorWindowBase
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

            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-us");

            DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EngineCore.Init(new WindowInteropHelper(this).Handle, (int)ActualWidth, (int)ActualHeight);

            EngineCore.CurrentScene = AssetsManager.LoadScene("Assets\\Scenes\\Scene5.xml");

            EngineCore.IsPaused = true;
            EngineCore.Run();
        }

        private void EditorWindowInst_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (EngineCore.IsAlive)
                EngineCore.Stop();
        }

        private void EditorWindowInst_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
        }

    }
}