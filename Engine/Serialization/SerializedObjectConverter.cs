using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Engine.Serialization
{
    public class SerializedObjectConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(SerializedObjectPromise);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            string idScalar = parser.Consume<Scalar>().Value;
            if (Guid.TryParseExact(idScalar, "N", out Guid instanceId))
            {
                return new SerializedObjectPromise(instanceId);
            }

            return null;
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            Guid? id = (value as SerializableObject)?.InstanceId;
            Scalar idScalar = new Scalar(id?.ToString("N") ?? string.Empty);
            emitter.Emit(idScalar);
        }
    }
}