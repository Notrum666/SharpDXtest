using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Engine;

namespace Editor
{
    public class FieldViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private FieldInfo targetField;
        public FieldInfo TargetField => targetField;
        public string DisplayName { get; private set; }
        public object Value
        {
            // TODO: optimize repetitive boxing-unboxing by caching structure value
            get
            {
                if (parentField is null)
                    return targetField.GetValue(parentObject);
                return targetField.GetValue(parentField.Value);
            }
            set
            {
                if (parentField is null)
                {
                    targetField.SetValue(parentObject, value);
                    if (parentObject is INotifyFieldChanged notifyFieldChanged)
                        notifyFieldChanged.OnFieldChanged(targetField);
                }
                else
                {
                    object box = parentField.Value;
                    targetField.SetValue(box, value);
                    parentField.Value = box;
                }
                OnPropertyChanged();
            }
        }
        public ObservableCollection<FieldViewModel> StructFieldViewModels { get; private set; } = null;
        private object parentObject;
        private FieldViewModel parentField;

        public FieldViewModel(object parentObject, FieldInfo field)
            : this(parentObject, field, null) { }

        public FieldViewModel(object parentObject, FieldInfo field, FieldViewModel parentField)
        {
            this.parentObject = parentObject;
            targetField = field;
            this.parentField = parentField;

            SerializeFieldAttribute attr;
            if ((attr = field.GetCustomAttribute<SerializeFieldAttribute>()) is not null)
            {
                if (attr.DisplayName is not null)
                    DisplayName = attr.DisplayName;
                else
                    DisplayName = field.Name;
            }
            else
                DisplayName = field.Name;

            if (targetField.FieldType.IsValueType && !targetField.FieldType.IsEnum && !targetField.FieldType.IsPrimitive)
            {
                StructFieldViewModels = new ObservableCollection<FieldViewModel>();
                FieldInfo[] subFields = targetField.FieldType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (FieldInfo subField in subFields)
                {
                    if (subField.IsPrivate && subField.GetCustomAttribute<SerializeFieldAttribute>() is null)
                        continue;
                    StructFieldViewModels.Add(new FieldViewModel(parentObject, subField, this));
                }
            }
        }

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public void Update()
        {
            if (StructFieldViewModels is null)
            {
                OnPropertyChanged(nameof(Value));
                return;
            }
            foreach (FieldViewModel fieldViewModel in StructFieldViewModels)
                fieldViewModel.Update();
        }
    }
}