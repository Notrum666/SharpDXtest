using System.Collections.ObjectModel;

using Engine;
using Engine.BaseAssets.Components;

namespace Editor
{
    public class GameObjectComponentsViewModel : ViewModelBase
    {
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

        public GameObjectComponentsViewModel() { }

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