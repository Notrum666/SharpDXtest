using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Engine;

using Component = Engine.BaseAssets.Components.Component;

namespace Editor
{
    public class GameObjectViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private GameObject target = null;
        public GameObject Target
        {
            get => target;
            set
            {
                target = value;
                OnPropertyChanged();
                Reload();
            }
        }
        public ObservableCollection<ComponentViewModel> ComponentViewModels { get; private set; } = new ObservableCollection<ComponentViewModel>();

        public GameObjectViewModel() { }

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public void Reload()
        {
            ComponentViewModels.Clear();
            foreach (Component component in target.Components)
                ComponentViewModels.Add(new ComponentViewModel(component));
        }

        public void Update()
        {
            foreach (ComponentViewModel viewModel in ComponentViewModels)
                viewModel.Update();
        }
    }
}