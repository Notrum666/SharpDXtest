using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
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
            var mainPath = Directory.GetCurrentDirectory();
            var solutionPath = Directory.GetParent(mainPath)?.Parent?.Parent?.Parent?.Parent?.FullName;
            AssetsManager.InitializeInFolder(solutionPath);
            AssetsRegistry.InitializeInFolder(solutionPath);
            
            EngineCore.Init(new WindowInteropHelper(this).Handle, (int)ActualWidth, (int)ActualHeight);

            CreateBaseScene();

            EngineCore.IsPaused = true;
            EngineCore.Run();
        }

        private void EditorWindowInst_Closing(object sender, CancelEventArgs e)
        {
            if (EngineCore.IsAlive)
                EngineCore.Stop();
        }

        private void EditorWindowInst_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
        }

        private void CreateBaseScene()
        {
            Scene.CurrentScene = new Scene();
            GameObject obj1 = GameObject.Instantiate("TestObject_1");
            GameObject obj2 = GameObject.Instantiate("TestObject_2", obj1.Transform);
        }
    }
}