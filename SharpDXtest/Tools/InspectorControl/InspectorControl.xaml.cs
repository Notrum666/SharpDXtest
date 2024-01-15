using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Engine;

using SharpDXtest;

namespace Editor
{
    /// <summary>
    /// Interaction logic for OutputControl.xaml
    /// </summary>
    public partial class InspectorControl : UserControl, INotifyPropertyChanged
    {
        internal static List<FieldDataTemplate> FieldDataTemplates { get; } = new List<FieldDataTemplate>();
        public event PropertyChangedEventHandler PropertyChanged;
        public static GameObjectComponentsViewModel GameObjectViewModel { get; private set; }
        private bool loaded = false;
        private int objectIndex = -1;

        static InspectorControl()
        {
            // protection against design-time class addressing
            if (Application.Current is not App)
                return;

            ResourceDictionary resourceDictionary = new ResourceDictionary();
            resourceDictionary.Source = new Uri("pack://application:,,,/Tools/InspectorControl/DefaultFieldTemplates.xaml", UriKind.RelativeOrAbsolute);
            foreach (object resource in resourceDictionary.Values)
            {
                if (resource is FieldDataTemplate template)
                    RegisterFieldDataTemplate(template);
            }
        }

        public static void RegisterFieldDataTemplate(FieldDataTemplate template)
        {
            if (FieldDataTemplates.Any(t => t.TargetType == template.TargetType))
                throw new ArgumentException("FieldDataTemplate for type " + template.TargetType.Name + " already registered.");

            FieldDataTemplates.Add(template);
        }

        public InspectorControl()
        {
            InitializeComponent();

            GameObjectViewModel ??= new GameObjectComponentsViewModel();

            DataContext = this;
        }

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // to prevent errors during xaml designer loading in visual studio
            if (!EngineCore.IsAlive)
                return;

            if (!loaded)
            {
                Width = double.NaN;
                Height = double.NaN;
            }

            loaded = true;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // to prevent errors during xaml designer loading in visual studio
            if (!EngineCore.IsAlive)
                return;

        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Focus();

            if (e.RightButton != MouseButtonState.Pressed)
                return;

            Scene currentScene = Scene.CurrentScene;
            if (currentScene == null || currentScene.GameObjects.Count == 0)
            {
                GameObjectViewModel.Target = null;
                return;
            }
            
            objectIndex++;

            if (objectIndex >= currentScene.GameObjects.Count)
                objectIndex = 0;
            GameObjectViewModel.Target = currentScene.GameObjects[objectIndex];
        }
    }
}