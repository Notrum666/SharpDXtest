using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Engine;
using Engine.AssetsData;

namespace Editor.AssetsImport
{
    public abstract class AssetImporter
    {
        [YamlTagMapped]
        public class BaseImportSettings { }

        protected abstract BaseImportSettings GetDefaultSettings();

        protected abstract AssetData OnImportAsset(string assetPath, AssetMeta assetMeta);

        public AssetMeta ImportAsset(string assetSourcePath)
        {
            ValidateExtension(assetSourcePath);
            string metaPath = GetMetaPath(assetSourcePath);
            string contentRelativePath = AssetsRegistry.GetContentAssetPath(assetSourcePath);

            AssetMeta assetMeta = LoadMetaFile(metaPath);
            DateTime? artifactImportDate = AssetsManager.GetAssetImportDate(contentRelativePath);

            bool metaOutOfDate = assetMeta.ImportDate < File.GetLastWriteTimeUtc(assetSourcePath);
            bool artifactOutOfDate = artifactImportDate == null || artifactImportDate < assetMeta.ImportDate;

            if (metaOutOfDate || artifactOutOfDate)
            {
                AssetData importedAsset = OnImportAsset(assetSourcePath, assetMeta);
                if (importedAsset == null)
                    return null;

                if (metaOutOfDate)
                    SaveMetaFile(metaPath, assetMeta);

                AssetsManager.SaveAsset(contentRelativePath, assetMeta.Guid, importedAsset);
            }

            return assetMeta;
        }

        protected bool TryGetTypedImportSettings<T>(AssetMeta assetMeta, out T importSettings) where T : class
        {
            importSettings = assetMeta.ImportSettings as T;
            return importSettings != null;
        }

        private AssetMeta LoadMetaFile(string metaPath)
        {
            AssetMeta savedMeta = YamlManager.LoadFromFile<AssetMeta>(metaPath);
            return savedMeta ?? new AssetMeta()
            {
                ImportSettings = GetDefaultSettings()
            };
        }

        private void SaveMetaFile(string metaPath, AssetMeta assetMeta)
        {
            assetMeta.ImportDate = DateTime.UtcNow;
            YamlManager.SaveToFile(metaPath, assetMeta);
        }

        private string GetMetaPath(string assetSourcePath)
        {
            string assetExtension = Path.GetExtension(assetSourcePath);
            return Path.ChangeExtension(assetSourcePath, $"{assetExtension}{AssetMeta.MetaExtension}");
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