using System;
using System.IO;
using System.Reflection;

using YamlDotNet.Serialization;

namespace Engine
{
    public static class YamlManager
    {
        private static ISerializer Serializer { get; }
        private static IDeserializer Deserializer { get; }

        static YamlManager()
        {
            Serializer = BuildSerializer();
            Deserializer = BuildDeserializer();
        }

        private static ISerializer BuildSerializer()
        {
            SerializerBuilder builder = new SerializerBuilder();

            builder.RegisterTagMappedClasses();

            return builder.Build();
        }

        private static IDeserializer BuildDeserializer()
        {
            DeserializerBuilder builder = new DeserializerBuilder();

            builder.RegisterTagMappedClasses();

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

        public static void SaveToFile<T>(string filePath, T obj)
        {
            using StreamWriter writer = new StreamWriter(filePath, false);
            Serializer.Serialize(writer, obj, typeof(T));
        }

        public static object LoadFromFile(string filePath, Type type)
        {
            if (!File.Exists(filePath))
                return null;

            using StreamReader reader = new StreamReader(filePath);
            return Deserializer.Deserialize(reader, type);
        }

        public static T LoadFromFile<T>(string filePath)
        {
            return (T)LoadFromFile(filePath, typeof(T));
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