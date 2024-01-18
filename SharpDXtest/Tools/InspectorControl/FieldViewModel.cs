using System;
using System.Collections.ObjectModel;
using System.Reflection;

using Engine;

using Windows.UI.Text;

namespace Editor
{
    public class FieldViewModel : ViewModelBase
    {
        private RelayCommand clearNullableValueCommand;
        public RelayCommand ClearNullableValueCommand => clearNullableValueCommand ??= 
            new RelayCommand(_ => Value = null, _ => Nullable.GetUnderlyingType(TargetType) is not null);
        private RelayCommand setDefaultNullableValueCommand;
        public RelayCommand SetDefaultNullableValueCommand => setDefaultNullableValueCommand ??= 
            new RelayCommand(_ => Value = Activator.CreateInstance(Nullable.GetUnderlyingType(TargetType)), 
                _ => Nullable.GetUnderlyingType(TargetType) is not null);
        private FieldInfo targetField;
        public FieldInfo TargetField => targetField;
        public Type TargetType { get; }
        public string DisplayName { get; }
        private FieldConverter converter = null;
        private bool propagateUpdate = false;
        private object cachedBoxedValue;
        private bool invalidated = true;
        private int arrayIndex = -1;
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
                object result = arrayIndex == -1 ? targetField.GetValue(parentField.Value) : ((Array)parentField.Value).GetValue(arrayIndex);
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
                    if (arrayIndex == -1)
                        targetField.SetValue(box, newValue);
                    else
                        ((Array)box).SetValue(newValue, arrayIndex);
                    parentField.Value = box;
                }
                if (propagateUpdate)
                    Update();
                else
                    OnPropertyChanged();
            }
        }
        private ObservableCollection<FieldViewModel> subFieldsViewModels = null;
        public ObservableCollection<FieldViewModel> SubFieldsViewModels
        {
            get => subFieldsViewModels;
            set
            {
                subFieldsViewModels = value;
                OnPropertyChanged();
            }
        }
        private object parentObject;
        private FieldViewModel parentField;

        public FieldViewModel(object parentObject, FieldInfo field)
            : this(parentObject, field, null) { }
        public FieldViewModel(object parentObject, FieldInfo field, FieldViewModel parentField)
            : this(parentObject, field, parentField, -1) { }
        private FieldViewModel(object parentObject, FieldInfo field, FieldViewModel parentField, int index)
        {
            this.parentObject = parentObject;
            this.parentField = parentField;
            if (field != null)
            {
                targetField = field;
                TargetType = targetField.FieldType;
                DisplayName = field.Name;

                SerializedFieldAttribute attr = targetField.GetCustomAttribute<SerializedFieldAttribute>();
                if (attr is not null)
                {
                    DisplayName = attr.NameOverride ?? field.Name;
                    DisplayOverridesAttribute overridesAttr = field.GetCustomAttribute<DisplayOverridesAttribute>();
                    if (overridesAttr is not null)
                    {
                        propagateUpdate = overridesAttr.PropagateStructUpdate;
                        if (overridesAttr.ConverterType is not null)
                        {
                            converter = (FieldConverter)Activator.CreateInstance(overridesAttr.ConverterType);
                            if (converter.SourceType != targetField.FieldType)
                            {
                                Logger.Log(LogType.Error, "Incorrect converter input type: " + converter.SourceType.Name + ", while field type is: " + targetField.FieldType);
                                TargetType = null;
                                return;
                            }
                            TargetType = converter.TargetType;
                        }
                    }
                }
            }
            else
            {
                TargetType = parentField.TargetType.GetElementType();
                DisplayName = index.ToString();
                arrayIndex = index;
            }

            RefreshSubElements();
        }

        private void RefreshSubElements()
        {
            if (TargetType.IsStruct())
            {
                if (SubFieldsViewModels is null)
                    SubFieldsViewModels = new ObservableCollection<FieldViewModel>();

                Type underlyingType = Nullable.GetUnderlyingType(targetField.FieldType);
                GenerateSubFieldsForType(underlyingType ?? TargetType);
            }

            if (TargetType.IsArray)
            {
                if (SubFieldsViewModels is null)
                    SubFieldsViewModels = new ObservableCollection<FieldViewModel>();

                invalidated = true;
                Array arr = Value as Array;
                int length = arr is not null ? arr.Length : 0;
                for (int i = SubFieldsViewModels.Count - 1; i >= length; i--)
                    SubFieldsViewModels.RemoveAt(i);
                for (int i = SubFieldsViewModels.Count; i < length; i++)
                    SubFieldsViewModels.Add(new FieldViewModel(parentObject, null, this, i));
            }
        }
        private void GenerateSubFieldsForType(Type type)
        {
            FieldInfo[] subFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo subField in subFields)
            {
                if (subField.IsPrivate && (subField.GetCustomAttribute<SerializedFieldAttribute>() is null ||
                    subField.GetCustomAttribute<HideInInspectorAttribute>() is not null))
                    continue;
                SubFieldsViewModels.Add(new FieldViewModel(parentObject, subField, this));
            }
        }

        public void Update()
        {
            if (SubFieldsViewModels is null)
            {
                invalidated = true;
                OnPropertyChanged(nameof(Value));
                return;
            }
            else
            {
                if (TargetType.IsArray)
                    RefreshSubElements();
            }
            foreach (FieldViewModel fieldViewModel in SubFieldsViewModels)
                fieldViewModel.Update();
        }
    }
}