using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Engine;

namespace Editor.AssetsImport
{
    public abstract class AssetImporter
    {
        public abstract int LatestVersion { get; }

        [YamlTagMapped]
        public class BaseImportSettings { }

        protected abstract void OnImportAsset(AssetImportContext importContext);

        public Guid ImportAsset(string assetSourcePath)
        {
            ValidateExtension(assetSourcePath);
            AssetImportContext importContext = new AssetImportContext(assetSourcePath);

            AssetMeta assetMeta = importContext.LoadAssetMeta();
            DateTime? artifactImportDate = AssetsManager.GetAssetImportDate(importContext.AssetContentPath);

            bool metaOutOfDate = assetMeta.ImporterVersion != LatestVersion;
            bool artifactOutOfDate = artifactImportDate == null
                                     || artifactImportDate < assetMeta.LastWriteTimeUtc
                                     || artifactImportDate < File.GetLastWriteTimeUtc(importContext.AssetSourcePath);

            if (metaOutOfDate || artifactOutOfDate)
            {
                using FileStream fileStream = File.OpenRead(assetSourcePath);
                importContext.DataStream = fileStream;

                OnImportAsset(importContext);

                assetMeta.ImporterVersion = LatestVersion;
                importContext.SaveAssetMeta();
            }

            return assetMeta.Guid;
        }

        private void ValidateExtension(string assetSourcePath)
        {
            string extension = Path.GetExtension(assetSourcePath);
            IEnumerable<string> extensions = GetType().GetCustomAttribute<AssetImporterAttribute>()?.Extensions ?? Enumerable.Empty<string>();
            Logger.Assert(extensions.Contains(extension));
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class AssetImporterAttribute : Attribute
    {
        public IEnumerable<string> Extensions { get; }

        public AssetImporterAttribute(params string[] ext)
        {
            Extensions = ext.Where(s => !string.IsNullOrWhiteSpace(s)).Select(x => $".{x}");
        }
    }
}