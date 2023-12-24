using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace Engine.Serialization
{
    public class SerializedFieldsTypeInspector : TypeInspectorSkeleton
    {
        private readonly ITypeResolver typeResolver = new SerializedObjectTypeResolver();

        public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object container)
        {
            IEnumerable<IPropertyDescriptor> serializedFields = GetSerializedFieldsDescriptors(type);
            return serializedFields;
        }

        private IEnumerable<IPropertyDescriptor> GetSerializedFieldsDescriptors(Type objectType)
        {
            List<IPropertyDescriptor> fields = new List<IPropertyDescriptor>();

            IEnumerable<(FieldInfo, SerializedFieldAttribute)> serializedFields = GetSerializedFields(objectType);
            foreach ((FieldInfo serializedField, SerializedFieldAttribute attribute) in serializedFields)
            {
                string serializedName = attribute?.NameOverride ?? serializedField.Name;
                fields.Add(new SerializedFieldDescriptor(serializedName, serializedField, typeResolver));
            }

            return fields;
        }

        private static IEnumerable<(FieldInfo, SerializedFieldAttribute)> GetSerializedFields(Type type)
        {
            if (type == null)
                return Enumerable.Empty<(FieldInfo, SerializedFieldAttribute)>();

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                                       BindingFlags.DeclaredOnly | BindingFlags.Instance;

            IEnumerable<(FieldInfo, SerializedFieldAttribute)> ownedFields = type.GetFields(flags)
                .Select(info => (info, info.GetCustomAttribute<SerializedFieldAttribute>()))
                .Where(x => x.info.IsPublic || x.Item2 != null);

            return GetSerializedFields(type.BaseType).Concat(ownedFields);
        }

        private sealed class SerializedFieldDescriptor : IPropertyDescriptor
        {
            public string Name => serializedName;
            public bool CanWrite => !fieldInfo.IsInitOnly;

            public Type Type => fieldInfo.FieldType;
            public Type TypeOverride { get; set; }

            public int Order { get; set; }
            public ScalarStyle ScalarStyle { get; set; }

            private readonly string serializedName;
            private readonly FieldInfo fieldInfo;
            private readonly ITypeResolver typeResolver;

            public SerializedFieldDescriptor(string serializedName, FieldInfo fieldInfo, ITypeResolver typeResolver)
            {
                this.serializedName = serializedName;
                this.fieldInfo = fieldInfo;
                this.typeResolver = typeResolver;
            }

            public T GetCustomAttribute<T>() where T : Attribute
            {
                return fieldInfo.GetCustomAttribute<T>(true);
            }

            public IObjectDescriptor Read(object target)
            {
                object propertyValue = fieldInfo.GetValue(target);
                Type actualType = TypeOverride ?? typeResolver.Resolve(Type, propertyValue);

                return new ObjectDescriptor(propertyValue, actualType, Type, ScalarStyle);
            }

            public void Write(object target, object value)
            {
                fieldInfo.SetValue(target, value);
            }
        }
    }
}