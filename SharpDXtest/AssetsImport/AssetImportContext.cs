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
            assetMetaData.SubAssets[subAssetKey] = subGuid;

            AssetsManager.SaveAssetData(AssetContentPath, subGuid, subAsset);
            return subGuid;
        }

        public T GetImportSettings<T>() where T : AssetImporter.BaseImportSettings
        {
            if (assetMetaData.ImportSettings is not T)
                assetMetaData.ImportSettings = Activator.CreateInstance<T>();

            return (T)assetMetaData.ImportSettings;
        }
    }
}