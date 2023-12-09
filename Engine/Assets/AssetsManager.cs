using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

using Engine.AssetsData;

namespace Engine
{
    //Stored files are called artifacts | loaded objects are assets
    public static class AssetsManager
    {
        public const string ArtifactFolderName = "Artifact";
        public static string ArtifactFolderPath { get; private set; }

        private const string ArtifactDatabaseName = "GuidDB.db";
        private static string ArtifactDatabasePath { get; set; }
        private static Dictionary<string, Dictionary<string, Type>> artifactDatabase = new Dictionary<string, Dictionary<string, Type>>();

        public static void InitializeInFolder(string parentFolderPath)
        {
            Logger.Log(LogType.Info, $"Initialize AssetManager in folder = {parentFolderPath}");

            ArtifactFolderPath = Path.Combine(parentFolderPath, ArtifactFolderName);
            Directory.CreateDirectory(ArtifactFolderPath);

            ArtifactDatabasePath = Path.Combine(ArtifactFolderPath, ArtifactDatabaseName);
            LoadArtifactDatabase();
        }

        private static void SaveArtifactDatabase()
        {
            YamlManager.SaveToFile(ArtifactDatabasePath, artifactDatabase);
        }

        private static void LoadArtifactDatabase()
        {
            artifactDatabase = YamlManager.LoadFromFile<Dictionary<string, Dictionary<string, Type>>>(ArtifactDatabasePath)
                               ?? new Dictionary<string, Dictionary<string, Type>>();
        }

        private static string GetArtifactPathFromGuid(string guid)
        {
            return Path.Combine(ArtifactFolderPath, $"{guid}.dat");
        }

        private static void SaveArtifact(string guid, AssetData assetData)
        {
            string artifactPath = GetArtifactPathFromGuid(guid);

            using FileStream fileStream = File.Open(artifactPath, FileMode.Create);
            using BinaryWriter binaryWriter = new BinaryWriter(fileStream, Encoding.UTF8, false);

            assetData.Serialize(binaryWriter);
        }

        [return: MaybeNull]
        private static AssetData LoadArtifact(string guid, Type type)
        {
            string artifactPath = GetArtifactPathFromGuid(guid);
            if (!Path.Exists(artifactPath))
                return null;

            using FileStream fileStream = File.Open(artifactPath, FileMode.Open);
            using BinaryReader binaryReader = new BinaryReader(fileStream, Encoding.UTF8, false);

            AssetData assetData = (AssetData)Activator.CreateInstance(type);
            assetData?.Deserialize(binaryReader);
            return assetData;
        }

        public static void SaveAsset(string contentAssetPath, string guid, AssetData assetData)
        {
            SaveArtifact(guid, assetData);

            if (!artifactDatabase.ContainsKey(contentAssetPath))
                artifactDatabase[contentAssetPath] = new Dictionary<string, Type>();
            artifactDatabase[contentAssetPath][guid] = assetData.GetType();
            SaveArtifactDatabase();
        }

        public static object LoadAssetByGuid(string guid, Type artifactType, Type assetType)
        {
            AssetData artifact = LoadArtifact(guid, artifactType);
            return artifact?.ToRealAsset(assetType);
        }

        public static object LoadAssetAtPath(string contentAssetPath, Type assetType)
        {
            if (!artifactDatabase.TryGetValue(contentAssetPath, out Dictionary<string, Type> savedArtifacts))
                return null;

            if (savedArtifacts.Count != 1)
            {
                Logger.Log(LogType.Warning, $"Found {savedArtifacts.Count} artifacts for asset at {contentAssetPath}");
                return null;
            }

            KeyValuePair<string, Type> firstArtifact = savedArtifacts.FirstOrDefault();
            return LoadAssetByGuid(firstArtifact.Key, firstArtifact.Value, assetType);
        }

        public static T LoadAssetAtPath<T>(string contentAssetPath)
        {
            return (T)LoadAssetAtPath(contentAssetPath, typeof(T));
        }

        //TODO: dependency collection (return false if could not delete safely)
        public static bool DeleteAsset(string contentAssetPath)
        {
            artifactDatabase.Remove(contentAssetPath);
            SaveArtifactDatabase();
            return true;
        }

        public static bool UpdateAssetPath(string oldContentAssetPath, string newContentAssetPath)
        {
            if (string.IsNullOrWhiteSpace(newContentAssetPath) || !artifactDatabase.ContainsKey(oldContentAssetPath))
                return false;

            if (artifactDatabase.ContainsKey(newContentAssetPath))
            {
                Logger.Log(LogType.Warning, $"Tried to update path = \"{oldContentAssetPath}\" to already existing path = \"{newContentAssetPath}\"");
                return false;
            }

            artifactDatabase[newContentAssetPath] = artifactDatabase[oldContentAssetPath];
            artifactDatabase.Remove(oldContentAssetPath);
            SaveArtifactDatabase();
            return true;
        }

        public static DateTime? GetAssetImportDate(string contentAssetPath)
        {
            if (!artifactDatabase.TryGetValue(contentAssetPath, out Dictionary<string, Type> savedArtifacts))
                return null;

            if (savedArtifacts.Count == 0)
                return null;

            string firstGuid = savedArtifacts.First().Key;
            string artifactPath = GetArtifactPathFromGuid(firstGuid);
            return File.GetLastWriteTimeUtc(artifactPath);
        }
    }
}