using System;
using System.Collections.Generic;

namespace Editor.AssetsImport
{
    public class AssetMeta
    {
        public const string MetaExtension = ".meta";
        public DateTime LastWriteTimeUtc { get; set; }

        public int ImporterVersion = -1;

        public Guid Guid = Guid.NewGuid();
        public Dictionary<(Type, string), Guid> SubAssets = new Dictionary<(Type, string), Guid>();

        public object ImportSettings = null;
    }
}