using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

using Engine;
using Engine.Assets;
using Engine.AssetsData;
using Engine.BaseAssets.Components;

namespace Editor
{
    /// <summary>
    /// Interaction logic for OutputControl.xaml
    /// </summary>
    public partial class SceneOverviewControl : UserControl
    {
        public static SceneOverviewControl Current {  get; private set; }
        private RelayCommand refreshCommand;
        public RelayCommand RefreshCommand => refreshCommand ??= new RelayCommand(_ => Refresh());

        public static readonly DependencyProperty SelectedGameObjectProperty = DependencyProperty.Register("SelectedGameObject", typeof(GameObject),
            typeof(SceneOverviewControl),
            new FrameworkPropertyMetadata(null, OnSelectedGameObjectPropertyChanged),
            new ValidateValueCallback(IsValidSelectedGameObject));
        public GameObject SelectedGameObject
        {
            get => (GameObject)GetValue(SelectedGameObjectProperty);
            set => SetValue(SelectedGameObjectProperty, value);
        }
        private static void OnSelectedGameObjectPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SceneOverviewControl control = (SceneOverviewControl)sender;

            if (e.NewValue is GameObject obj)
            {
                if ((control.SceneTreeView.SelectedItem as GameObjectTreeViewModel)?.GameObject != obj)
                    control.SelectObject(obj);
            }
            else
                control.DeselectObject();
        }
        private static bool IsValidSelectedGameObject(object obj)
        {
            return obj is null || obj is GameObject;
        }

        public SceneViewModel SceneViewModel { get; } = new SceneViewModel();
        private bool loaded = false;
        internal GameObject ObjectToSelect { get; set; }

        private bool isGrabbed = false;
        private Point localGrabPos;
        private readonly double DragThreshold = 5;

        private DispatcherTimer UpdateTimer;
        public SceneOverviewControl()
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
            bool wasInvalidated = SceneViewModel.Invalidated;
            SceneViewModel.Update();
            if (wasInvalidated && ObjectToSelect is not null)
            {
                EditorLayer.Current.SelectedGameObject = ObjectToSelect;
                ObjectToSelect = null;
            }
        }

        public void Refresh()
        {
            SceneViewModel.Refresh();
        }

        private void OnSceneLoaded(string name)
        {
            SceneViewModel.Scene = Scene.CurrentScene;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // to prevent errors during xaml designer loading in visual studio
            if (!EngineCore.IsAlive)
                return;

            if (!loaded)
            {
                BindingOperations.SetBinding(this, SelectedGameObjectProperty, new Binding()
                {
                    Path = new PropertyPath("(0).SelectedGameObject", typeof(EditorLayer).GetProperty(nameof(EditorLayer.Current)))
                });

                Width = double.NaN;
                Height = double.NaN;
            }

            if (Scene.CurrentScene is not null)
                SceneViewModel.Scene = Scene.CurrentScene;
            SceneManager.OnSceneLoaded += OnSceneLoaded;

            Transform.ParentChanged += Transform_ParentChanged;

            Current = this;

            loaded = true;
        }

        private void Transform_ParentChanged(Transform obj)
        {
            SceneViewModel.Invalidate();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // to prevent errors during xaml designer loading in visual studio
            if (!EngineCore.IsAlive)
                return;

            SceneManager.OnSceneLoaded -= OnSceneLoaded;
            SceneViewModel.Scene = null;

            Transform.ParentChanged -= Transform_ParentChanged;

            Current = null;
        }

        public void SelectObject(GameObject gameObject)
        {
            GameObjectTreeViewModel viewModel = FindViewModel(gameObject);
            if (viewModel is null)
            {
                Logger.Log(LogType.Error, "Tried to select object that is not on the scene");
                return;
            }
            ChangeObjectViewModelSelection(viewModel, true);
        }
        public void DeselectObject()
        {
            if (SceneTreeView.SelectedItem is GameObjectTreeViewModel viewModel)
                ChangeObjectViewModelSelection(viewModel, false);
        }
        private GameObjectTreeViewModel FindViewModel(GameObject obj)
        {
            foreach (GameObjectTreeViewModel viewModel in SceneViewModel.GameObjectViewModels)
                if (TryFindViewModel(viewModel, obj, out GameObjectTreeViewModel res))
                    return res;
            return null;
        }
        private bool TryFindViewModel(GameObjectTreeViewModel viewModel, GameObject obj, out GameObjectTreeViewModel res)
        {
            if (viewModel.GameObject == obj)
            {
                res = viewModel;
                return true;
            }
            foreach (GameObjectTreeViewModel subViewModel in viewModel.Children)
                if (TryFindViewModel(subViewModel, obj, out res))
                    return true;
            res = null;
            return false;
        }
        internal TreeViewItem FindVisibleTreeViewItem(GameObjectTreeViewModel gameobjectViewModel)
        {
            Stack<GameObjectTreeViewModel> queue = new Stack<GameObjectTreeViewModel>();
            queue.Push(gameobjectViewModel);
            GameObjectTreeViewModel parent = gameobjectViewModel.Parent;
            while (parent != null)
            {
                queue.Push(parent);
                parent = parent.Parent;
            }

            ItemContainerGenerator generator = SceneTreeView.ItemContainerGenerator;
            while (queue.Count > 0)
            {
                GameObjectTreeViewModel dequeue = queue.Pop();
                //SceneTreeView.UpdateLayout();
                TreeViewItem treeViewItem = (TreeViewItem)generator.ContainerFromItem(dequeue);
                if (queue.Count == 0)
                    return treeViewItem;
                generator = treeViewItem.ItemContainerGenerator;
            }
            return null;
        }
        private void ChangeObjectViewModelSelection(GameObjectTreeViewModel gameobjectViewModel, bool desiredSelectionState)
        {
            Stack<GameObjectTreeViewModel> queue = new Stack<GameObjectTreeViewModel>();
            queue.Push(gameobjectViewModel);
            GameObjectTreeViewModel parent = gameobjectViewModel.Parent;
            while (parent != null)
            {
                queue.Push(parent);
                parent = parent.Parent;
            }

            ItemContainerGenerator generator = SceneTreeView.ItemContainerGenerator;
            while (queue.Count > 0)
            {
                GameObjectTreeViewModel dequeue = queue.Pop();
                SceneTreeView.UpdateLayout();
                TreeViewItem treeViewItem = (TreeViewItem)generator.ContainerFromItem(dequeue);
                if (queue.Count > 0)
                    treeViewItem.IsExpanded = true;
                else
                    treeViewItem.IsSelected = desiredSelectionState;
                generator = treeViewItem.ItemContainerGenerator;
            }
        }

        private void SceneTreeView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            EditorLayer.Current.SelectedGameObject = null;
        }

        private void FocusSelfOnLoad(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            textBox.Focus();
            textBox.SelectAll();
        }

        private void SceneTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
                return;

            if (EditorLayer.Current.SelectedGameObject is not null)
            {
                EditorLayer.Current.SelectedGameObject.Destroy();
                EditorLayer.Current.SelectedGameObject = null;
            }

            e.Handled = true;
        }

        private void SceneTreeView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            EditorLayer.Current.SelectedGameObject = (SceneTreeView.SelectedItem as GameObjectTreeViewModel)?.GameObject;
        }

        private void GameObjectTreeViewModel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                isGrabbed = true;
                localGrabPos = e.GetPosition(this);
            }
        }

        private void GameObjectTreeViewModel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
                isGrabbed = false;

            e.Handled = true;

            if (isGrabbed && (e.GetPosition(this) - localGrabPos).LengthSquared >= DragThreshold * DragThreshold)
            {
                GameObjectTreeViewModel dataContext = (GameObjectTreeViewModel)((FrameworkElement)sender).DataContext;
                DragDropEffects effects = DragDropEffects.Move | DragDropEffects.Link;
                DragDrop.DoDragDrop(this, new DataObject(DataFormats.Serializable, dataContext.GameObject), effects);
            }
        }

        private void GameObjectTreeViewModel_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            object data = e.Data.GetData(DataFormats.Serializable);
            if (data is GameObject)
            {
                e.Effects = DragDropEffects.Move;
                return;
            }
            if (data is ContentBrowserAssetViewModel assetViewModel && assetViewModel.AssociatedAssetDataType == typeof(PrefabData))
            {
                e.Effects = DragDropEffects.Copy;
                return;
            }
            e.Effects = DragDropEffects.None;
        }

        private void GameObjectTreeViewModel_Drop(object sender, DragEventArgs e)
        {
            GameObject target = ((GameObjectTreeViewModel)((FrameworkElement)sender).DataContext).GameObject;
            if (target is null)
                return;
            object obj = e.Data.GetData(DataFormats.Serializable);
            if (obj is GameObject gameObject)
            {
                gameObject.Transform.SetParent(target.Transform);
                e.Handled = true;
                return;
            }
            if (obj is ContentBrowserAssetViewModel assetViewModel && assetViewModel.AssociatedAssetDataType == typeof(PrefabData))
            {
                AssetsManager.LoadAssetByGuid<Prefab>(assetViewModel.AssetMeta.Guid).NewInstantiate(target.Transform);
                e.Handled = true;
                return;
            }
        }

        private void SceneTreeView_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            object data = e.Data.GetData(DataFormats.Serializable);
            if (data is GameObject)
            {
                e.Effects = DragDropEffects.Move;
                return;
            }
            if (data is ContentBrowserAssetViewModel assetViewModel && assetViewModel.AssociatedAssetDataType == typeof(PrefabData))
            {
                e.Effects = DragDropEffects.Copy;
                return;
            }
            e.Effects = DragDropEffects.None;
        }

        private void SceneTreeView_Drop(object sender, DragEventArgs e)
        {
            object obj = e.Data.GetData(DataFormats.Serializable);
            if (obj is GameObject gameObject)
            {
                gameObject.Transform.SetParent(null);
                Refresh();
                e.Handled = true;
                return;
            }
            if (obj is ContentBrowserAssetViewModel assetViewModel && assetViewModel.AssociatedAssetDataType == typeof(PrefabData))
            {
                AssetsManager.LoadAssetByGuid<Prefab>(assetViewModel.AssetMeta.Guid).NewInstantiate();
                Refresh();
                e.Handled = true;
                return;
            }
        }
    }
}