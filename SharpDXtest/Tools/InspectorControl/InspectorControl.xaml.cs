using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

using Editor.AssetsImport;

using Engine;
using Engine.BaseAssets.Components;

using SharpDXtest;

using Component = Engine.BaseAssets.Components.Component;

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
                if (assetViewModel.TargetObject is null)
                    return;
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

        private RelayCommand openAddComponentPopup;
        public RelayCommand OpenAddComponentPopup => openAddComponentPopup ??= new RelayCommand(
            popup =>
            {
                ((Popup)popup).IsOpen = true;
            }
        );
        private RelayCommand closeAddComponentPopup;
        public RelayCommand CloseAddComponentPopup => closeAddComponentPopup ??= new RelayCommand(
            popup =>
            {
                ((Popup)popup).IsOpen = false;
            }
        );
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

        private List<Type> componentTypes = new List<Type>();
        public ReadOnlyCollection<Type> ComponentTypes => componentTypes.AsReadOnly();
        private static readonly Type[] ComponentsBlacklist = new Type[]
        {
            typeof(Transform)
        };

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

        private void ReloadTypes()
        {
            componentTypes.Clear();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName.Contains("Editor") || a.FullName.Contains("Engine"))
                .Concat(AssemblyLoadContext.CurrentContextualReflectionContext.Assemblies))
                componentTypes.AddRange(assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Component)) && !t.IsAbstract));

            componentTypes.RemoveAll(t => ComponentsBlacklist.Contains(t));

            componentTypes.Sort((a, b) => a.Name.CompareTo(b.Name));
        }

        public void Reload()
        {
            TargetObjectViewModel?.Reload();
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

                BindingOperations.SetBinding(this, TargetObjectProperty, new Binding()
                {
                    Path = new PropertyPath("(0).InspectedObject", typeof(EditorLayer).GetProperty(nameof(EditorLayer.Current)))
                });
            }

            ReloadTypes();
            ScriptManager.OnCodeRecompiled += ReloadTypes;

            loaded = true;

            current = this;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // to prevent errors during xaml designer loading in visual studio
            if (!EngineCore.IsAlive)
                return;

            ScriptManager.OnCodeRecompiled -= ReloadTypes;

            current = null;
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Focus();
        }
    }
}