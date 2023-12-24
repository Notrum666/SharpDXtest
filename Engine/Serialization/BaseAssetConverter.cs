using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Engine.Serialization
{
    public class BaseAssetConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(BaseAsset) || type.IsSubclassOf(typeof(BaseAsset));
        }

        public object ReadYaml(IParser parser, Type type)
        {
            string guid = parser.Consume<Scalar>().Value;
            return string.IsNullOrEmpty(guid) ? null : AssetsManager.LoadAssetByGuid(guid, type);
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            BaseAsset asset = value as BaseAsset;
            Scalar guidScalar = new Scalar(asset?.Guid ?? string.Empty);
            emitter.Emit(guidScalar);
        }
    }
}