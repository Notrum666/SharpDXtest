using System;
using YamlDotNet.Serialization;

namespace Engine.Serialization
{
    public class SerializedObjectTypeResolver : ITypeResolver
    {
        public Type Resolve(Type staticType, object actualValue)
        {
            if (staticType.IsSubclassOf(typeof(SerializableObject)))
                return typeof(SerializedObjectPromise);

            return actualValue == null ? staticType : actualValue.GetType();
        }
    }
}