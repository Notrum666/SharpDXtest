using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Engine.Serialization
{
    public class BaseAssetConverter : IYamlTypeConverter
    {
        private readonly GuidConverter guidConverter;

        public BaseAssetConverter(GuidConverter guidConverter)
        {
            this.guidConverter = guidConverter;
        }

        public bool Accepts(Type type)
        {
            return type == typeof(BaseAsset) || type.IsSubclassOf(typeof(BaseAsset));
        }

        public object ReadYaml(IParser parser, Type type)
        {
            string guidValue = parser.Consume<Scalar>().Value;

            if (string.IsNullOrEmpty(guidValue))
                return null;

            Guid guid = guidConverter.Parse(guidValue);
            return AssetsManager.LoadAssetByGuid(guid, type);
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            if (value is BaseAsset asset)
            {
                guidConverter.WriteYaml(emitter, asset.Guid, typeof(Guid));
                return;
            }

            emitter.Emit(new Scalar(string.Empty));
        }
    }
}