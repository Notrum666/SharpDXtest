using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Engine;
using Engine.BaseAssets.Components;

namespace Editor
{
    public class GameObjectViewModel : InspectorObjectViewModel
    {
        private RelayCommand addComponentCommand;
        public RelayCommand AddComponentCommand => addComponentCommand ??= new RelayCommand(
            obj =>
            {
                Target.AddComponent((Type)obj);
            }
        );
        private RelayCommand removeComponentCommand;
        public RelayCommand RemoveComponentCommand => removeComponentCommand ??= new RelayCommand(
            obj =>
            {
                ((Component)obj).Destroy();
            }
        );

        private GameObject target = null;
        public GameObject Target => target;
        public ObservableCollection<ObjectViewModel> ComponentsViewModels { get; private set; } = new ObservableCollection<ObjectViewModel>();
        public string Name
        {
            get => target.Name;
            set
            {
                target.Name = value;
                OnPropertyChanged();
            }
        }
        public bool Enabled
        {
            get => Target.LocalEnabled;
            set
            {
                Target.LocalEnabled = value;
                OnPropertyChanged();
            }
        }

        public GameObjectViewModel(GameObject target) 
        {
            this.target = target;
            Reload();
        }

        public override void Reload()
        {
            ComponentsViewModels.Clear();

            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Enabled));

            foreach (Component component in target.Components)
                ComponentsViewModels.Add(new ObjectViewModel(component));
        }

        public override void Update()
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Enabled));
            if (!target.Components.SequenceEqual(ComponentsViewModels.Select(c => c.Target)))
                Reload();
            else
                foreach (ObjectViewModel viewModel in ComponentsViewModels)
                    viewModel.Update();
        }
    }
}