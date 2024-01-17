﻿using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

using Engine;
using Engine.BaseAssets.Components;

namespace Editor
{
    public class GameObjectViewModel : InspectorObjectViewModel
    {
        private GameObject target = null;
        public GameObject Target => target;
        public ObservableCollection<ObjectViewModel> ComponentsViewModels { get; private set; } = new ObservableCollection<ObjectViewModel>();

        public GameObjectViewModel(GameObject target) 
        {
            this.target = target;
            Reload();
        }

        public override void Reload()
        {
            ComponentsViewModels.Clear();

            foreach (Component component in target.Components)
                ComponentsViewModels.Add(new ObjectViewModel(component));
        }

        public override void Update()
        {
            foreach (ObjectViewModel viewModel in ComponentsViewModels)
                viewModel.Update();
        }
    }
}