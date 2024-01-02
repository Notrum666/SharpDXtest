using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Engine.AssetsData;

namespace Engine
{
    //Stored files are called artifacts | loaded objects are assets
    public static class AssetsManager
    {
        public const string ArtifactFolderName = "Artifact";
        public static string ArtifactFolderPath { get; private set; }

        /// <summary> BaseAsset subtype to AssetData subtype </summary>
        private static readonly Dictionary<Type, Type> typeToDataTypeMap = new Dictionary<Type, Type>();

        private static readonly Dictionary<Guid, WeakReference<BaseAsset>> assetsCache = new Dictionary<Guid, WeakReference<BaseAsset>>();
        private static ArtifactDatabase artifactDatabase;

        static AssetsManager()
        {
            SetupAssetTypesMap();
        }

        private static void SetupAssetTypesMap()
        {
            Type baseType = typeof(BaseAsset);
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type type in types)
            {
                Attribute importerAttribute = type.GetCustomAttribute(typeof(AssetDataAttribute<>));
                if (importerAttribute != null)
                {
                    if (!type.IsSubclassOf(typeof(AssetData)))
                    {
                        Logger.Log(LogType.Warning, $"Type \"{type.Name}\" has AssetDataAttribute attribute but is not derived from AssetData");
                        continue;
                    }

                    Type assetType = importerAttribute.GetType().GetGenericArguments()[0];
                    if (!assetType.IsSubclassOf(baseType) && assetType != baseType)
                    {
                        Logger.Log(LogType.Warning, $"Type \"{type.Name}\" has AssetDataAttribute attribute but its target type \"{assetType.Name}\" is not derived from BaseAsset");
                        continue;
                    }

                    typeToDataTypeMap[assetType] = type;
                }
            }
        }

        public static void InitializeInFolder(string parentFolderPath)
        {
            Logger.Log(LogType.Info, $"Initialize AssetManager in folder = {parentFolderPath}");

            ArtifactFolderPath = Path.Combine(parentFolderPath, ArtifactFolderName);
            Directory.CreateDirectory(ArtifactFolderPath);

            artifactDatabase = ArtifactDatabase.Load(ArtifactFolderPath);
        }

        private static string GetArtifactFullPath(Guid guid)
        {
            return Path.Combine(ArtifactFolderPath, $"{guid:N}.dat");
        }

        private static bool TryGetAssetDataType(Type assetType, out Type assetDataType)
        {
            if (typeToDataTypeMap.TryGetValue(assetType, out assetDataType))
                return true;

            Logger.Log(LogType.Warning, $"No AssetData subtype registered for Asset type \"{assetType.Name}\"");
            return false;
        }

        private static void CacheAsset(BaseAsset asset)
        {
            assetsCache[asset.Guid] = new WeakReference<BaseAsset>(asset);
        }

        private static bool TryGetCachedAsset(Guid guid, out BaseAsset asset)
        {
            if (assetsCache.TryGetValue(guid, out WeakReference<BaseAsset> weakRef))
                return weakRef.TryGetTarget(out asset);

            asset = null;
            return false;
        }

        private static void SaveArtifact(Guid guid, AssetData assetData)
        {
            string artifactPath = GetArtifactFullPath(guid);

            using FileStream fileStream = File.Open(artifactPath, FileMode.Create);
            using BinaryWriter binaryWriter = new BinaryWriter(fileStream, Encoding.UTF8, false);

            assetData.Serialize(binaryWriter);
        }

        [return: MaybeNull]
        private static AssetData LoadArtifact(Guid guid, Type assetDataType)
        {
            string artifactPath = GetArtifactFullPath(guid);
            if (!Path.Exists(artifactPath))
                return null;

            using FileStream fileStream = File.Open(artifactPath, FileMode.Open);
            using BinaryReader binaryReader = new BinaryReader(fileStream, Encoding.UTF8, false);

            AssetData assetData = (AssetData)Activator.CreateInstance(assetDataType);
            assetData?.Deserialize(binaryReader);
            return assetData;
        }

        private static BaseAsset LoadAsset(Guid guid, Type assetDataType)
        {
            if (TryGetCachedAsset(guid, out BaseAsset asset))
                return asset;

            AssetData artifact = LoadArtifact(guid, assetDataType);
            asset = artifact?.ToRealAsset(guid);
            CacheAsset(asset);

            return asset;
        }

        public static void SaveAssetData(string contentAssetPath, Guid guid, AssetData assetData)
        {
            SaveArtifact(guid, assetData);
            artifactDatabase.SaveArtifactData(contentAssetPath, guid, assetData.GetType());
        }

        /// <summary>
        /// Returns asset object of requested type with specified guid
        /// </summary>
        /// <param name="guid">Guid of the asset to load</param>
        /// <param name="expectedAssetType">When not null - checks if the asset is of specified type</param>
        /// <returns>The asset matching the parameters or null</returns>
        public static BaseAsset LoadAssetByGuid(Guid guid, Type expectedAssetType = null)
        {
            if (!artifactDatabase.TryGetArtifactType(guid, out Type assetDataType))
                return null;

            if (expectedAssetType != null)
            {
                if (!TryGetAssetDataType(expectedAssetType, out Type expectedDataType))
                    return null;

                if (assetDataType != expectedDataType)
                {
                    Logger.Log(LogType.Warning, $"Tried to get asset \"{guid}\" with type \"{expectedAssetType.Name}\", but another type found");
                    return null;
                }
            }

            return LoadAsset(guid, assetDataType);
        }

        /// <inheritdoc cref="LoadAssetByGuid(Guid, Type)"/>
        public static T LoadAssetByGuid<T>(Guid guid) where T : BaseAsset
        {
            return (T)LoadAssetByGuid(guid, typeof(T));
        }

        /// <summary>
        /// Returns the first asset object of requested type at given path
        /// </summary>
        /// <param name="contentAssetPath">Content relative path of the asset to load</param>
        /// <param name="assetType">Should be subclass of BaseAsset</param>
        /// <returns>The asset matching the parameters or null</returns>
        public static BaseAsset LoadAssetAtPath(string contentAssetPath, Type assetType)
        {
            if (!assetType.IsSubclassOf(typeof(BaseAsset)))
            {
                Logger.Log(LogType.Warning, $"Requested asset type \"{assetType.Name}\" is not derived from BaseAsset");
                return null;
            }

            if (!TryGetAssetDataType(assetType, out Type requestedDataType))
                return null;

            List<Guid> foundGuids = artifactDatabase.GetGuids(contentAssetPath, false);
            foreach (Guid guid in foundGuids)
            {
                if (artifactDatabase.TryGetArtifactType(guid, out Type assetDataType) && assetDataType == requestedDataType)
                    return LoadAsset(guid, assetDataType);
            }

            Logger.Log(LogType.Warning, $"No asset of type \"{assetType.Name}\" found at path \"{contentAssetPath}\"");
            return null;
        }

        /// <inheritdoc cref="LoadAssetAtPath(string, Type)"/>
        public static T LoadAssetAtPath<T>(string contentAssetPath) where T : BaseAsset
        {
            return (T)LoadAssetAtPath(contentAssetPath, typeof(T));
        }

        /// <summary>
        /// Returns all asset objects at given path
        /// </summary>
        /// <param name="contentAssetPath">Content relative path of the asset to load</param>
        /// <param name="assetType">Should be subclass of BaseAsset</param>
        /// <returns>All assets matching the parameters or empty array</returns>
        public static BaseAsset[] LoadAllAssetsAtPath(string contentAssetPath)
        {
            List<Guid> foundGuids = artifactDatabase.GetGuids(contentAssetPath, true);
            List<BaseAsset> assets = new List<BaseAsset>();

            foreach (Guid guid in foundGuids)
            {
                if (artifactDatabase.TryGetArtifactType(guid, out Type assetDataType))
                {
                    BaseAsset asset = LoadAsset(guid, assetDataType);
                    assets.Add(asset);
                }
            }

            return assets.ToArray();
        }

        //TODO: dependency collection (return false if could not delete safely)
        public static bool DeleteAsset(string contentAssetPath)
        {
            return artifactDatabase.DeleteArtifactsData(contentAssetPath);
        }

        public static bool UpdateAssetPath(string oldContentAssetPath, string newContentAssetPath)
        {
            return artifactDatabase.UpdateArtifactsPath(oldContentAssetPath, newContentAssetPath);
        }

        public static DateTime? GetAssetImportDate(string contentAssetPath)
        {
            List<Guid> guids = artifactDatabase.GetGuids(contentAssetPath, false);

            if (guids.Count == 0)
                return null;

            string artifactPath = GetArtifactFullPath(guids.First());
            return File.GetLastWriteTimeUtc(artifactPath);
        }
    }

    public class ArtifactDatabase
    {
        private const int LatestVersion = 2;

        private const string FileName = "GuidDB.db";

        [SerializedField]
        private readonly int version;
        [SerializedField]
        private readonly Dictionary<string, List<Guid>> pathToGuidsMap = new Dictionary<string, List<Guid>>();
        [SerializedField]
        private readonly Dictionary<Guid, Type> guidToDataTypeMap = new Dictionary<Guid, Type>();

        private string filePath;

        public ArtifactDatabase()
        {
            version = LatestVersion;
        }

        internal static ArtifactDatabase Load(string artifactFolderPath)
        {
            string databaseFilePath = Path.Combine(artifactFolderPath, FileName);

            ArtifactDatabase artifactDatabase = YamlManager.LoadFromFile<ArtifactDatabase>(databaseFilePath);

            if (artifactDatabase == null || artifactDatabase.version != LatestVersion)
                artifactDatabase = new ArtifactDatabase();

            artifactDatabase.filePath = databaseFilePath;
            return artifactDatabase;
        }

        private void Save()
        {
            YamlManager.SaveToFile(filePath, this);
        }

        internal void SaveArtifactData(string contentAssetPath, Guid guid, Type assetDataType)
        {
            if (!pathToGuidsMap.TryGetValue(contentAssetPath, out List<Guid> guids))
            {
                guids = new List<Guid>();
                pathToGuidsMap[contentAssetPath] = guids;
            }

            if (!guids.Contains(guid))
                guids.Add(guid);

            guidToDataTypeMap[guid] = assetDataType;

            Save();
        }

        internal List<Guid> GetGuids(string contentAssetPath, bool warningIfNone)
        {
            if (!pathToGuidsMap.TryGetValue(contentAssetPath, out List<Guid> foundGuids))
                foundGuids = new List<Guid>();

            if (warningIfNone && foundGuids.Count == 0)
                Logger.Log(LogType.Warning, $"No registered assets found at path \"{contentAssetPath}\"");

            return foundGuids;
        }

        internal bool TryGetArtifactType(Guid guid, out Type dataType)
        {
            if (guidToDataTypeMap.TryGetValue(guid, out dataType))
                return true;

            Logger.Log(LogType.Warning, $"No registered assetData type found for guid \"{guid}\"");
            return false;
        }

        internal bool DeleteArtifactsData(string contentAssetPath)
        {
            if (!pathToGuidsMap.TryGetValue(contentAssetPath, out List<Guid> guids))
                return false;

            foreach (Guid guid in guids)
                guidToDataTypeMap.Remove(guid);

            pathToGuidsMap.Remove(contentAssetPath);

            Save();
            return true;
        }

        internal bool UpdateArtifactsPath(string oldContentAssetPath, string newContentAssetPath)
        {
            if (string.IsNullOrWhiteSpace(newContentAssetPath) || !pathToGuidsMap.ContainsKey(oldContentAssetPath))
                return false;

            if (pathToGuidsMap.ContainsKey(newContentAssetPath))
            {
                Logger.Log(LogType.Warning, $"Tried to update path = \"{oldContentAssetPath}\" to already existing path = \"{newContentAssetPath}\"");
                return false;
            }

            pathToGuidsMap[newContentAssetPath] = pathToGuidsMap[oldContentAssetPath];
            pathToGuidsMap.Remove(oldContentAssetPath);

            Save();
            return true;
        }
    }
}