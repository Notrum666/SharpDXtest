using System;
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
        public Type TargetType { get; }
        public string DisplayName { get; }
        private FieldConverter converter = null;
        private bool propagateUpdate = false;
        private object cachedBoxedValue;
        private bool invalidated = true;
        public object Value
        {
            get
            {
                if (parentField is null)
                {
                    if (invalidated)
                    {
                        cachedBoxedValue = targetField.GetValue(parentObject);
                        if (converter is not null)
                            cachedBoxedValue = converter.Convert(cachedBoxedValue);
                        invalidated = false;
                    }
                    return cachedBoxedValue;
                }
                object result = targetField.GetValue(parentField.Value);
                if (converter is not null)
                    result = converter.Convert(result);
                return result;
            }
            set
            {
                object newValue = value;
                if (converter is not null)
                    newValue = converter.ConvertBack(newValue);
                if (parentField is null)
                {
                    invalidated = true;
                    targetField.SetValue(parentObject, newValue);
                    if (parentObject is INotifyFieldChanged notifyFieldChanged)
                        notifyFieldChanged.OnFieldChanged(targetField);
                }
                else
                {
                    object box = parentField.Value;
                    targetField.SetValue(box, newValue);
                    parentField.Value = box;
                }
                if (propagateUpdate)
                    Update();
                else
                    OnPropertyChanged();
            }
        }
        public ObservableCollection<FieldViewModel> StructFieldViewModels { get; } = null;
        private object parentObject;
        private FieldViewModel parentField;

        public FieldViewModel(object parentObject, FieldInfo field)
            : this(parentObject, field, null) { }

        public FieldViewModel(object parentObject, FieldInfo field, FieldViewModel parentField)
        {
            this.parentObject = parentObject;
            targetField = field;
            this.parentField = parentField;
            TargetType = targetField.FieldType;
            DisplayName = field.Name;

            DisplayFieldAttribute attr;
            if ((attr = field.GetCustomAttribute<DisplayFieldAttribute>()) is not null)
            {
                DisplayName = attr.DisplayName;
                propagateUpdate = attr.PropagateStructUpdate;
                if (attr.ConverterType is not null)
                {
                    converter = (FieldConverter)Activator.CreateInstance(attr.ConverterType);
                    if (converter.SourceType != targetField.FieldType)
                    {
                        Logger.Log(LogType.Error, "Incorrect converter input type: " + converter.SourceType.Name + ", while field type is: " + targetField.FieldType);
                        TargetType = null;
                        return;
                    }
                    TargetType = converter.TargetType;
                }
            }

            if (targetField.FieldType.IsStruct())
            {
                StructFieldViewModels = new ObservableCollection<FieldViewModel>();
                FieldInfo[] subFields = TargetType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (FieldInfo subField in subFields)
                {
                    if (subField.IsPrivate && subField.GetCustomAttribute<DisplayFieldAttribute>() is null)
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
                invalidated = true;
                OnPropertyChanged(nameof(Value));
                return;
            }
            foreach (FieldViewModel fieldViewModel in StructFieldViewModels)
                fieldViewModel.Update();
        }
    }
}