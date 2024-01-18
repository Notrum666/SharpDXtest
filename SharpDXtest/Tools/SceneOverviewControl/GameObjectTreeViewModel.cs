using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

using Engine;
using Engine.BaseAssets.Components;

namespace Editor
{
    public class GameObjectTreeViewModel : ViewModelBase
    {
        private RelayCommand startGameObjectCreationCommand;
        public RelayCommand StartGameObjectCreationCommand => startGameObjectCreationCommand ??= new RelayCommand(obj =>
        {
            EditorLayer.Current.SelectedGameObject = null;
            TreeViewItem item = SceneOverviewControl.Current.FindVisibleTreeViewItem(this);
            item.IsExpanded = true;
            GameObjectCreationTreeViewModel newViewModel = new GameObjectCreationTreeViewModel(this);
            GameObjectCreationTreeViewModels.Add(newViewModel);
            SceneOverviewControl.Current.SceneTreeView.UpdateLayout();
        });
        public ObservableCollection<GameObjectTreeViewModel> Children { get; } = new ObservableCollection<GameObjectTreeViewModel>();
        public ObservableCollection<GameObjectCreationTreeViewModel> GameObjectCreationTreeViewModels { get; } = new ObservableCollection<GameObjectCreationTreeViewModel>();
        public CompositeCollection SubItems { get; }
        public GameObject GameObject { get; }
        private string cachedName;
        public string Name
        {
            get => cachedName;
            set
            {
                GameObject.Name = value;
                cachedName = GameObject.Name;
                OnPropertyChanged();
            }
        }
        public GameObjectTreeViewModel Parent { get; }
        public GameObjectTreeViewModel(GameObject gameObject)
            : this(gameObject, null) { }
        public GameObjectTreeViewModel(GameObject gameObject, GameObjectTreeViewModel parent)
        {
            SubItems = new CompositeCollection
            {
                new CollectionContainer() { Collection = Children },
                new CollectionContainer() { Collection = GameObjectCreationTreeViewModels }
            };

            GameObject = gameObject;
            foreach (Transform transform in gameObject.Transform.Children)
                Children.Add(new GameObjectTreeViewModel(transform.GameObject, this));
            Parent = parent;
            Name = gameObject.Name;
        }
        public void Update()
        {
            foreach (GameObjectTreeViewModel child in Children)
                child.Update();
            if (Name != GameObject.Name)
                Name = GameObject.Name;
        }
    }
}
