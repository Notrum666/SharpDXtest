using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using Engine.BaseAssets.Components;
using Engine.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;
using YamlDotNet.Serialization.Utilities;

namespace Engine
{
    public static class YamlManager
    {
        private static ISerializer Serializer { get; }
        private static IDeserializer Deserializer { get; }

        private static CustomObjectFactory ObjectFactory { get; }
        private static BaseAssetConverter BaseAssetConverter { get; }
        private static SerializedObjectConverter SerializedObjectConverter { get; }

        static YamlManager()
        {
            BaseAssetConverter = new BaseAssetConverter();
            ObjectFactory = new CustomObjectFactory(new DefaultObjectFactory());

            SerializedObjectConverter = new SerializedObjectConverter();
            TypeConverter.RegisterTypeConverter<SerializableObject, SerializedObjectPromiseConverter>();

            Serializer = BuildSerializer();
            Deserializer = BuildDeserializer();
        }

        private static ISerializer BuildSerializer()
        {
            SerializerBuilder builder = new SerializerBuilder();
            builder.EnsureRoundtrip();

            builder.RegisterTagMappedClasses();
            builder.WithTypeConverter(BaseAssetConverter);

            builder.WithTypeResolver(new SerializedObjectTypeResolver());
            builder.WithTypeInspector(_ => new SerializedFieldsTypeInspector());
            builder.WithTypeConverter(SerializedObjectConverter);

            return builder.Build();
        }

        private static IDeserializer BuildDeserializer()
        {
            DeserializerBuilder builder = new DeserializerBuilder();

            builder.RegisterTagMappedClasses();
            builder.WithObjectFactory(ObjectFactory);
            builder.WithTypeConverter(BaseAssetConverter);

            builder.WithTypeResolver(new SerializedObjectTypeResolver());
            builder.WithTypeInspector(_ => new SerializedFieldsTypeInspector());
            builder.WithNodeTypeResolver(new SerializedObjectNodeTypeResolver());
            builder.WithTypeConverter(SerializedObjectConverter);

            return builder.Build();
        }

        private static BuilderSkeleton<T> RegisterTagMappedClasses<T>(this BuilderSkeleton<T> builder) where T : BuilderSkeleton<T>
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    YamlTagMappedAttribute tagMappedAttribute = type.GetCustomAttribute<YamlTagMappedAttribute>();
                    if (tagMappedAttribute != null)
                    {
                        string tagName = tagMappedAttribute.TagName ?? type.Name;
                        builder.WithTagMapping($"!{tagName}", type);
                    }
                }
            }

            return builder;
        }

        public static void SaveToStream<T>(Stream stream, T obj)
        {
            using StreamWriter writer = new StreamWriter(stream, leaveOpen: true);
            Serializer.Serialize(writer, obj, typeof(T));
        }

        public static void SaveToFile<T>(string filePath, T obj)
        {
            using FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            SaveToStream(fileStream, obj);
        }

        public static object LoadFromStream(Stream stream, Type type)
        {
            using StreamReader reader = new StreamReader(stream, leaveOpen: true);
            return Deserializer.Deserialize(reader, type);
        }

        public static T LoadFromStream<T>(Stream stream)
        {
            return (T)LoadFromStream(stream, typeof(T));
        }

        public static void LoadFromStream<T>(Stream stream, T instance)
        {
            ObjectFactory.SetObjectInstance(typeof(T), instance);

            using StreamReader reader = new StreamReader(stream, leaveOpen: true);
            Deserializer.Deserialize(reader, typeof(T));
        }

        public static object LoadFromFile(string filePath, Type type)
        {
            if (!File.Exists(filePath))
                return null;

            using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return LoadFromStream(fileStream, type);
        }

        public static T LoadFromFile<T>(string filePath)
        {
            return (T)LoadFromFile(filePath, typeof(T));
        }

        private class CustomObjectFactory : IObjectFactory
        {
            private readonly Dictionary<Type, object> instancedObjects = new Dictionary<Type, object>();
            private readonly IObjectFactory fallback;

            public CustomObjectFactory(IObjectFactory fallback)
            {
                this.fallback = fallback;
            }

            public void SetObjectInstance(Type type, object instance)
            {
                instancedObjects[type] = instance;
            }

            public void ClearObjectInstance(Type type)
            {
                instancedObjects.Remove(type);
            }

            public object Create(Type type)
            {
                if (instancedObjects.TryGetValue(type, out object instance))
                {
                    ClearObjectInstance(type);
                    return instance;
                }
                
                if (type == typeof(GameObject) || type == typeof(Transform))
                    return FormatterServices.GetUninitializedObject(type);

                return fallback.Create(type);
            }

            public object CreatePrimitive(Type type)
            {
                return fallback.CreatePrimitive(type);
            }

            public bool GetDictionary(IObjectDescriptor descriptor, out IDictionary dictionary, out Type[] genericArguments)
            {
                return fallback.GetDictionary(descriptor, out dictionary, out genericArguments);
            }

            public Type GetValueType(Type type)
            {
                return fallback.GetValueType(type);
            }

            public void ExecuteOnDeserializing(object value)
            {
                fallback.ExecuteOnDeserializing(value);
            }

            public void ExecuteOnDeserialized(object value)
            {
                fallback.ExecuteOnDeserialized(value);
            }

            public void ExecuteOnSerializing(object value)
            {
                fallback.ExecuteOnSerializing(value);
            }

            public void ExecuteOnSerialized(object value)
            {
                fallback.ExecuteOnSerialized(value);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public class YamlTagMappedAttribute : Attribute
    {
        public string TagName { get; }

        public YamlTagMappedAttribute(string tagName = null)
        {
            TagName = tagName;
        }
    }
}