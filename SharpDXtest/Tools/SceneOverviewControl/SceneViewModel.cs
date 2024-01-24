using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

namespace Editor
{
    public class SceneViewModel : ViewModelBase
    {
        private RelayCommand startGameObjectCreationCommand;
        public RelayCommand StartGameObjectCreationCommand => startGameObjectCreationCommand ??= new RelayCommand(obj =>
        {
            EditorLayer.Current.SelectedGameObject = null;
            GameObjectCreationTreeViewModels.Add(new GameObjectCreationTreeViewModel(this));
        });
        public ObservableCollection<GameObjectTreeViewModel> GameObjectViewModels { get; } = new ObservableCollection<GameObjectTreeViewModel>();
        public ObservableCollection<GameObjectCreationTreeViewModel> GameObjectCreationTreeViewModels { get; } = new ObservableCollection<GameObjectCreationTreeViewModel>();
        public CompositeCollection SubItems { get; }
        private Scene scene = null;
        public Scene Scene
        {
            get => scene;
            set
            {
                if (scene is not null)
                {
                    scene.OnGameObjectRemoved -= OnSceneStateChanged;
                    scene.OnGameObjectAdded -= OnSceneStateChanged;
                }
                scene = value;
                if (scene is not null)
                {
                    scene.OnGameObjectRemoved += OnSceneStateChanged;
                    scene.OnGameObjectAdded += OnSceneStateChanged;
                }
                invalidated = true;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Name));
            }
        }
        public string Name => Scene?.Name ?? "(None)";
        private bool invalidated = false;
        public bool Invalidated => invalidated;
        public SceneViewModel()
        {
            SubItems = new CompositeCollection
            {
                new CollectionContainer() { Collection = GameObjectViewModels },
                new CollectionContainer() { Collection = GameObjectCreationTreeViewModels }
            };
        }
        private void OnSceneStateChanged(GameObject gameObject)
        {
            invalidated = true;
        }
        public void Invalidate()
        {
            invalidated = true;
        }
        public void Update()
        {
            if (invalidated)
                Refresh();
            else
                foreach (GameObjectTreeViewModel item in GameObjectViewModels)
                    item.Update();
        }
        public void Refresh()
        {
            invalidated = false;
            GameObjectViewModels.Clear();
            if (scene is not null)
                foreach (GameObject gameObject in scene.GameObjects.ToImmutableList())
                    if (gameObject.Transform.Parent is null)
                        GameObjectViewModels.Add(new GameObjectTreeViewModel(gameObject));
        }
    }
}
