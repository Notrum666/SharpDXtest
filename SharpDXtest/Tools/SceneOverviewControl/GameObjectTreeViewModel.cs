using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Engine;
using Engine.BaseAssets.Components;

namespace Editor.Tools.SceneOverviewControl
{
    public class GameObjectTreeViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<GameObjectTreeViewModel> Children { get; } = new ObservableCollection<GameObjectTreeViewModel>();
        public GameObject GameObject { get; }
        public string Name => GameObject.ToString();
        public GameObjectTreeViewModel(GameObject gameObject)
        {
            GameObject = gameObject;
            foreach (Transform transform in gameObject.Transform.Children)
                Children.Add(new GameObjectTreeViewModel(transform.GameObject));
        }
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
