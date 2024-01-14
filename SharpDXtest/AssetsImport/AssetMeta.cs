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
        
        /// <summary>
        /// Sub asset can only be displayed in ContentBrowser, but cannot be edited or referenced manually
        /// </summary>
        public readonly Dictionary<(Type, string), Guid> SubAssets = new Dictionary<(Type, string), Guid>();
        
        /// <summary>
        /// Exported asset is a separate asset, which data is updated from main's import
        /// </summary>
        public readonly Dictionary<(Type, string), Guid> ExportedAssets = new Dictionary<(Type, string), Guid>();

        public object ImportSettings = null;
    }
}