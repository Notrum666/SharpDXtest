using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using Engine;

using SharpDXtest;

namespace Editor
{
    /// <summary>
    /// Interaction logic for OutputControl.xaml
    /// </summary>
    public partial class InspectorControl : UserControl, INotifyPropertyChanged
    {
        private static InspectorControl current = null;
        public static InspectorControl Current => current;

        public static readonly DependencyProperty TargetObjectProperty = DependencyProperty.Register("TargetObject", typeof(object), 
            typeof(InspectorControl),
            new FrameworkPropertyMetadata(null, OnTargetObjectPropertyChanged),
            new ValidateValueCallback(IsValidTargetObject));
        public object TargetObject
        {
            get => GetValue(TargetObjectProperty);
            set => SetValue(TargetObjectProperty, value);
        }
        private static void OnTargetObjectPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            InspectorControl control = (InspectorControl)sender;
            if (e.NewValue is GameObject gameObject)
            {
                control.TargetObjectViewModel = new GameObjectViewModel(gameObject);
                return;
            }

            if (e.NewValue is InspectorAssetViewModel assetViewModel)
            {
                control.TargetObjectViewModel = new AssetObjectViewModel(assetViewModel);
                return;
            }

            if (e.NewValue is not null)
                control.TargetObjectViewModel = new ObjectViewModel(e.NewValue);
            else
                control.TargetObjectViewModel = null;
        }
        private static bool IsValidTargetObject(object obj)
        {
            return obj is null || obj.GetType().IsClass;
        }

        internal static List<FieldDataTemplate> FieldDataTemplates { get; } = new List<FieldDataTemplate>();
        public event PropertyChangedEventHandler PropertyChanged;
        private InspectorObjectViewModel targetObjectViewModel;
        public InspectorObjectViewModel TargetObjectViewModel
        {
            get => targetObjectViewModel;
            private set
            {
                targetObjectViewModel = value;
                OnPropertyChanged();
            }
        }
        private bool loaded = false;
        private int objectIndex = -1;

        private DispatcherTimer UpdateTimer;

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
            if (FieldDataTemplates.Any(t => t.TargetType == template.TargetType && t.Predicate is null))
                throw new ArgumentException("FieldDataTemplate for type " + template.TargetType.Name + " without predicate is already registered.");

            FieldDataTemplates.Add(template);
        }

        public InspectorControl()
        {
            InitializeComponent();

            DataContext = this;

            UpdateTimer = new DispatcherTimer();
            UpdateTimer.Interval = TimeSpan.FromSeconds(0.1);
            UpdateTimer.Tick += UpdateTick;
            UpdateTimer.Start();
        }

        private void UpdateTick(object sender, EventArgs e)
        {
            if (targetObjectViewModel is not null)
                targetObjectViewModel.Update();
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

            current = this;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // to prevent errors during xaml designer loading in visual studio
            if (!EngineCore.IsAlive)
                return;

            current = null;
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
                TargetObject = null;
                return;
            }
            
            objectIndex++;

            if (objectIndex >= currentScene.GameObjects.Count)
                objectIndex = 0;
            TargetObject = currentScene.GameObjects[objectIndex];
        }
    }
}