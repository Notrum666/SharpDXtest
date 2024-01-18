using System.Collections.ObjectModel;
using System.Reflection;

using Engine;
using Engine.BaseAssets.Components;

namespace Editor
{
    public class ObjectViewModel : InspectorObjectViewModel
    {
        private object target = null;
        public object Target => target;
        public ObservableCollection<FieldViewModel> FieldViewModels { get; private set; } = new ObservableCollection<FieldViewModel>();
        public string DisplayName => target.GetType().Name;

        public ObjectViewModel(object target)
        {
            this.target = target;
            Reload();
        }

        public override void Reload()
        {
            FieldInfo[] fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo field in fields)
            {
                if (!field.IsPublic && (field.GetCustomAttribute<SerializedFieldAttribute>() is null ||
                    field.GetCustomAttribute<HideInInspectorAttribute>() is not null))
                    continue;
                FieldViewModels.Add(new FieldViewModel(target, field));
            }
        }

        public override void Update()
        {
            foreach (FieldViewModel viewModel in FieldViewModels)
                viewModel.Update();
        }
    }
}