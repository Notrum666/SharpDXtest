using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Engine.Serialization
{
    public class GuidConverter : IYamlTypeConverter
    {
        private const string Format = "N";

        public bool Accepts(Type type)
        {
            return type == typeof(Guid);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            string value = parser.Consume<Scalar>().Value;
            return Parse(value);
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            Guid guid = (Guid)value;
            emitter.Emit(new Scalar(guid.ToString(Format)));
        }

        public Guid Parse(string value)
        {
            return Guid.ParseExact(value, Format);
        }
    }
}