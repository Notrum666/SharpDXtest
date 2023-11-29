using Engine;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

using Component = Engine.BaseAssets.Components.Component;

namespace Editor
{
    public class ComponentViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private Component target = null;
        public Component Target
        {
            get => target;
        }
        public ObservableCollection<FieldViewModel> FieldViewModels { get; private set; } = new ObservableCollection<FieldViewModel>();
        public string DisplayName
        {
            get
            {
                return target.GetType().Name;
            }
        }
        public ComponentViewModel(Component target)
        {
            this.target = target;
            Reload();
        }
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        public void Reload()
        {
            FieldInfo[] fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo field in fields)
            {
                if (field.IsPrivate && field.GetCustomAttribute<DisplayFieldAttribute>() is null)
                    continue;
                FieldViewModels.Add(new FieldViewModel(target, field));
            }
        }
        public void Update()
        {
            foreach (FieldViewModel viewModel in FieldViewModels)
                viewModel.Update();
        }
    }
}
