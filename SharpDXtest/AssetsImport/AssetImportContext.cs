using System;
using System.Collections.Generic;
using System.IO;

using Engine;
using Engine.AssetsData;

namespace Editor.AssetsImport
{
    public class AssetImportContext
    {
        public readonly string AssetSourcePath;
        public readonly string AssetContentPath;

        public Stream DataStream;

        private string assetMetaPath;
        private AssetMeta assetMetaData;

        private readonly Dictionary<(Type, string), Guid> subAssets = new Dictionary<(Type, string), Guid>();
        private readonly Dictionary<(Type, string), Guid> exportedAssets = new Dictionary<(Type, string), Guid>();

        public AssetImportContext(string assetSourcePath)
        {
            AssetSourcePath = assetSourcePath;
            AssetContentPath = AssetsRegistry.GetContentAssetPath(assetSourcePath);
        }

        public AssetMeta LoadAssetMeta()
        {
            string assetExtension = Path.GetExtension(AssetSourcePath);
            assetMetaPath = Path.ChangeExtension(AssetSourcePath, $"{assetExtension}{AssetMeta.MetaExtension}");

            AssetMeta savedMeta = YamlManager.LoadFromFile<AssetMeta>(assetMetaPath);
            if (assetMetaPath != null && savedMeta != null)
            {
                savedMeta.LastWriteTimeUtc = File.GetLastWriteTimeUtc(assetMetaPath);
            }
            else
            {
                savedMeta = new AssetMeta() { LastWriteTimeUtc = DateTime.MaxValue };
            }

            assetMetaData = savedMeta;
            return assetMetaData;
        }

        public void SaveAssetMeta(DateTime importTimeUtc, int importerVersion)
        {
            assetMetaData.ImporterVersion = importerVersion;

            assetMetaData.SubAssets.Clear();
            assetMetaData.SubAssets.AddRange(subAssets);

            assetMetaData.ExportedAssets.Clear();
            assetMetaData.ExportedAssets.AddRange(exportedAssets);

            YamlManager.SaveToFile(assetMetaPath, assetMetaData);
            File.SetLastWriteTimeUtc(assetMetaPath, importTimeUtc);
        }

        public Guid AddMainAsset<T>(T mainAsset) where T : AssetData
        {
            Guid guid = assetMetaData.Guid;

            AssetsManager.SaveAssetData(AssetContentPath, guid, mainAsset);
            return guid;
        }

        public Guid AddSubAsset<T>(string identifier, T subAsset) where T : AssetData
        {
            (Type, string) subAssetKey = (typeof(T), identifier);

            Guid subGuid = assetMetaData.SubAssets.GetValueOrDefault(subAssetKey, Guid.NewGuid());
            subAssets[subAssetKey] = subGuid;

            AssetsManager.SaveAssetData(AssetContentPath, subGuid, subAsset);
            return subGuid;
        }

        public Guid SaveExportedAsset<T>(string identifier, T subAsset) where T : NativeAssetData
        {
            (Type, string) externalAssetKey = (typeof(T), identifier);
            Guid externalGuid = assetMetaData.ExportedAssets.GetValueOrDefault(externalAssetKey, Guid.Empty);

            if (externalGuid != Guid.Empty && AssetsRegistry.TryGetAssetPath(externalGuid, out string assetPath))
            {
                Guid? savedGuid = AssetsRegistry.SaveAsset(assetPath, subAsset);
                return savedGuid.GetValueOrDefault(Guid.Empty);
            }

            string sourceAssetName = Path.GetFileNameWithoutExtension(AssetSourcePath);
            string sourceAssetFolder = Path.GetDirectoryName(AssetSourcePath)!;
            Guid? createdGuid = AssetsRegistry.CreateAsset($"{sourceAssetName}_{identifier}", sourceAssetFolder, subAsset);
            return createdGuid.GetValueOrDefault(Guid.Empty);
        }

        public Guid? GetExternalAssetGuid<T>(string relativeFilePath) where T : AssetData
        {
            string sourceAssetFolder = Path.GetDirectoryName(AssetSourcePath)!;
            string externalFilePath = Path.Combine(sourceAssetFolder, relativeFilePath);

            return AssetsRegistry.ImportAsset(externalFilePath);
        }

        public T GetImportSettings<T>() where T : AssetImporter.BaseImportSettings
        {
            if (assetMetaData.ImportSettings is not T)
                assetMetaData.ImportSettings = Activator.CreateInstance<T>();

            return (T)assetMetaData.ImportSettings;
        }
    }
}