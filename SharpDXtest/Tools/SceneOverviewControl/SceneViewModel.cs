using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Engine;

namespace Editor.Tools.SceneOverviewControl
{
    public class SceneViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<GameObjectTreeViewModel> GameObjectViewModels { get; } = new ObservableCollection<GameObjectTreeViewModel>();
        private Scene scene = null;
        public Scene Scene
        {
            get => scene;
            set
            {
                GameObjectViewModels.Clear();
                scene = value;
                OnPropertyChanged();
                if (scene is not null)
                    foreach (GameObject gameObject in scene.Objects)
                        if (gameObject.Transform.Parent is null)
                            GameObjectViewModels.Add(new GameObjectTreeViewModel(gameObject));
            }
        }
        public SceneViewModel()
        {

        }
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
