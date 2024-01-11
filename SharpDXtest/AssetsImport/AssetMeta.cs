using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Editor.AssetsImport
{
    public class AssetMeta
    {
        public const string MetaExtension = ".meta";

        public Guid Guid = Guid.NewGuid();
        public DateTime ImportDate = DateTime.MinValue;
        
        public Dictionary<(Type, string), Guid> SubAssets = new Dictionary<(Type, string), Guid>();

        public object ImportSettings = null;
    }
}