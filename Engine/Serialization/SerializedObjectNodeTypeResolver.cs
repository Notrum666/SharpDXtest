using System;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Engine.Serialization
{
    public class SerializedObjectNodeTypeResolver : INodeTypeResolver
    {
        public bool Resolve(NodeEvent nodeEvent, ref Type currentType)
        {
            if (currentType.IsSubclassOf(typeof(SerializedObject)))
            {
                currentType = typeof(SerializedObjectPromise);
                return true;
            }

            return false;
        }

    }
}