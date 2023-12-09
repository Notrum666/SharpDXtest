using System;

namespace Editor.AssetsImport
{
    public class AssetMeta
    {
        public const string MetaExtension = ".meta";

        public string Guid = System.Guid.NewGuid().ToString("N");
        public DateTime ImportDate = DateTime.MinValue;

        public object ImportSettings = null;
    }
}