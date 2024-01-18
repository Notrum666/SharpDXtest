using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

using Engine;
using Engine.BaseAssets.Components;

namespace Editor
{
    public class GameObjectCreationTreeViewModel : ViewModelBase
    {
        private RelayCommand gameObjectCreationTextBlock_LostFocusCommand;
        public RelayCommand GameObjectCreationTextBlock_LostFocusCommand => gameObjectCreationTextBlock_LostFocusCommand ??= new RelayCommand(obj =>
        {
            Destroy();
        });
        private RelayCommand gameObjectCreationTextBlock_KeyDownCommand;
        public RelayCommand GameObjectCreationTextBlock_KeyDownCommand => gameObjectCreationTextBlock_KeyDownCommand ??= new RelayCommand(obj =>
        {
            Tuple<object, EventArgs> data = obj as Tuple<object, EventArgs>;
            Key key = ((KeyEventArgs)data.Item2).Key;
            if (key == Key.Escape)
                Destroy();
            if (key != Key.Enter)
                return;
            if (string.IsNullOrWhiteSpace(name))
            {
                Destroy();
                return;
            }
            if (Scene.CurrentScene is null)
                return;
            GameObject parent = null;
            if (parentViewModel is GameObjectTreeViewModel gameObjectTreeViewModel)
                parent = gameObjectTreeViewModel.GameObject;
            SceneOverviewControl.Current.ObjectToSelect = GameObject.Instantiate(name, parent?.Transform);
            Destroy();
        });
        private void Destroy()
        {
            if (parentViewModel is GameObjectTreeViewModel gameObjectTreeViewModel)
                gameObjectTreeViewModel.GameObjectCreationTreeViewModels.Clear();
            else
                ((SceneViewModel)parentViewModel).GameObjectCreationTreeViewModels.Clear();
        }
        private string name = "NewGameObject";
        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }
        private object parentViewModel;
        public GameObjectCreationTreeViewModel(SceneViewModel scene)
        {
            parentViewModel = scene;
        }
        public GameObjectCreationTreeViewModel(GameObjectTreeViewModel parent)
        {
            parentViewModel = parent;
        }
    }
}
