using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Editor.AssetsImport
{
    public class AssetMeta
    {
        public const string MetaExtension = ".meta";

        public string Guid = System.Guid.NewGuid().ToString("N");
        public DateTime ImportDate = DateTime.MinValue;
        
        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
        public Dictionary<(Type, string), string> SubAssets = new Dictionary<(Type, string), string>();

        public object ImportSettings = null;
    }
}