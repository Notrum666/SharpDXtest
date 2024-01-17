using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Editor.AssetsImport;

using Engine;
using Engine.AssetsData;

using static Engine.FileSystemHelper;

namespace Editor
{
    public static class AssetsRegistry
    {
        public const string ContentFolderName = "Content";
        public static string ContentFolderPath { get; private set; }

        private static readonly Dictionary<string, AssetImporter> assetImporters = new Dictionary<string, AssetImporter>();

        private static readonly Dictionary<Guid, string> guidToPathMap = new Dictionary<Guid, string>();

        static AssetsRegistry()
        {
            SetupImporters();
        }

        private static void SetupImporters()
        {
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type type in types)
            {
                AssetImporterAttribute importerAttribute = type.GetCustomAttribute<AssetImporterAttribute>();
                if (importerAttribute != null)
                {
                    if (!type.IsSubclassOf(typeof(AssetImporter)))
                    {
                        Logger.Log(LogType.Warning, $"Type \"{type.Name}\" has AssetImporterAttribute attribute but is not derived from AssetImporter");
                        continue;
                    }

                    AssetImporter importer = Activator.CreateInstance(type) as AssetImporter;
                    foreach (string extension in importerAttribute.Extensions)
                    {
                        assetImporters[extension] = importer;
                    }
                }
            }
        }

        public static void InitializeInFolder(string parentFolderPath)
        {
            Logger.Log(LogType.Info, $"Initialize AssetRegistry in folder = {parentFolderPath}");

            ContentFolderPath = Path.Combine(parentFolderPath, ContentFolderName);
            Directory.CreateDirectory(ContentFolderPath);

            Refresh();
        }

        public static void Refresh()
        {
            guidToPathMap.Clear();
            foreach (PathInfo pathInfo in EnumeratePathInfoEntries(ContentFolderPath, "*.meta", true))
            {
                if (pathInfo.IsDirectory)
                    continue;

                string assetPath = Path.ChangeExtension(pathInfo.FullPath, null);
                if (!Path.Exists(assetPath))
                {
                    File.Delete(pathInfo.FullPath);
                    continue;
                }

                AssetMeta assetMeta = YamlManager.LoadFromFile<AssetMeta>(pathInfo.FullPath);
                if (!guidToPathMap.TryAdd(assetMeta.Guid, assetPath))
                {
                    File.Delete(pathInfo.FullPath);
                }
            }

            ImportFolder(ContentFolderPath);
        }

        public static bool TryGetAssetPath(Guid guid, out string assetPath)
        {
            return guidToPathMap.TryGetValue(guid, out assetPath);
        }

        public static bool TryGetAssetMetaPath(string assetPath, out string metaPath)
        {
            string assetExtension = Path.GetExtension(assetPath);
            metaPath = Path.ChangeExtension(assetPath, $"{assetExtension}{AssetMeta.MetaExtension}");
            return Path.Exists(metaPath);
        }

        #region AssetOperations //Import, Copy, Move, Rename, Delete

        public static Guid? ImportAsset(string assetPath)
        {
            if (!Path.Exists(assetPath))
                return null;

            string assetExtension = Path.GetExtension(assetPath);
            if (!assetImporters.TryGetValue(assetExtension, out AssetImporter importer))
                return null;

            Guid guid = importer.ImportAsset(assetPath);
            guidToPathMap[guid] = assetPath;
            return guid;
        }

        public static Guid? SaveAsset<T>(string assetPath, T assetData) where T : NativeAssetData
        {
            using FileStream fileStream = File.Open(assetPath, FileMode.Create);
            using BinaryWriter binaryWriter = new BinaryWriter(fileStream, Encoding.UTF8, false);

            assetData.Serialize(binaryWriter);
            binaryWriter.Flush();
            binaryWriter.Close();

            return ImportAsset(assetPath);
        }

        public static Guid? CreateAsset<T>(string assetName, string parentFolderPath, T assetData = null) where T : NativeAssetData
        {
            assetData ??= NativeAssetData.CreateDefault<T>();
            assetName = SanitizeFileName(assetName, true);

            string pathNoExtension = Path.Combine(parentFolderPath, assetName);
            string newAssetPath = Path.ChangeExtension(pathNoExtension, assetData.FileExtension);
            newAssetPath = GenerateUniquePath(newAssetPath);

            Directory.CreateDirectory(parentFolderPath);

            return SaveAsset<T>(newAssetPath, assetData);
        }
        public static NativeAssetData LoadNativeAsset(string assetPath)
        {
            if (!Path.Exists(assetPath))
                return null;

            NativeAssetData assetData = YamlManager.LoadFromFile<NativeAssetData>(assetPath);
            return assetData;
        }

        /// <summary>
        /// Copies asset and imports it with new meta
        /// </summary>
        /// <param name="assetPath">Full path to source asset file</param>
        /// <param name="newAssetPath">Full path to future asset file</param>
        public static bool CopyAsset(string assetPath, string newAssetPath)
        {
            if (!Path.Exists(assetPath))
                return false;

            newAssetPath = GenerateUniquePath(newAssetPath);
            File.Copy(assetPath, newAssetPath);
            return ImportAsset(newAssetPath) != null;
        }

        /// <summary>
        /// Moves asset and its meta to new path
        /// </summary>
        /// <param name="oldAssetPath">Full path to asset file</param>
        /// <param name="newAssetPath">New full path to asset file</param>
        public static bool MoveAsset(string oldAssetPath, string newAssetPath)
        {
            if (!Path.Exists(oldAssetPath) || Path.Exists(newAssetPath))
                return false;

            string oldContentAssetPath = GetContentAssetPath(oldAssetPath);
            string newContentAssetPath = GetContentAssetPath(newAssetPath);

            if (!IsSupportedAssetFile(oldAssetPath) || !AssetsManager.UpdateAssetPath(oldContentAssetPath, newContentAssetPath))
                return false;

            File.Move(oldAssetPath, newAssetPath);

            if (TryGetAssetMetaPath(oldAssetPath, out string oldMetaPath))
            {
                AssetMeta assetMeta = YamlManager.LoadFromFile<AssetMeta>(oldMetaPath);
                guidToPathMap[assetMeta.Guid] = newAssetPath;

                TryGetAssetMetaPath(newAssetPath, out string newMetaPath);
                File.Move(oldMetaPath, newMetaPath);
            }

            return true;
        }

        /// <summary>
        /// Renames asset and its meta
        /// </summary>
        /// <param name="assetPath">Full path to asset file</param>
        /// <param name="newAssetName">New name for asset and meta files</param>
        /// <returns></returns>
        public static bool RenameAsset(string assetPath, string newAssetName)
        {
            FileInfo fileInfo = new FileInfo(assetPath);

            string newAssetPath = Path.Combine(fileInfo.DirectoryName ?? string.Empty, $"{newAssetName}{fileInfo.Extension}");
            return MoveAsset(assetPath, newAssetPath);
        }

        public static bool DeleteAsset(string assetPath)
        {
            if (!Path.Exists(assetPath))
                return false;

            string contentAssetPath = GetContentAssetPath(assetPath);

            if (IsSupportedAssetFile(assetPath) && !AssetsManager.DeleteAsset(contentAssetPath))
                return false;

            File.Delete(assetPath);

            if (TryGetAssetMetaPath(assetPath, out string metaPath))
            {
                AssetMeta assetMeta = YamlManager.LoadFromFile<AssetMeta>(metaPath);
                guidToPathMap.Remove(assetMeta.Guid);
                File.Delete(metaPath);
            }

            return true;
        }

        #endregion AssetOperations

        #region FolderOperations //Import, Create, Copy, Move, Rename, Delete

        public static void ImportFolder(string folderPath)
        {
            if (!Path.Exists(folderPath))
                return;
            Logger.Log(LogType.Info, $"Folder import at {folderPath}");

            foreach (PathInfo pathInfo in EnumeratePathInfoEntries(folderPath, "*", true))
            {
                if (!pathInfo.IsDirectory)
                    ImportAsset(pathInfo.FullPath);
            }
        }

        public static void CreateFolder(string parentFolderPath, string newFolderName)
        {
            string fullPath = Path.Combine(parentFolderPath, newFolderName);
            fullPath = GenerateUniquePath(fullPath);
            Directory.CreateDirectory(fullPath);
        }

        public static bool CopyFolder(string folderPath, string newFolderPath)
        {
            if (!Path.Exists(folderPath))
                return false;

            newFolderPath = GenerateUniquePath(newFolderPath);
            Directory.CreateDirectory(newFolderPath);

            foreach (PathInfo pathInfo in EnumeratePathInfoEntries(folderPath, "*", true))
            {
                if (!pathInfo.IsDirectory)
                    continue;
                string relativePath = Path.GetRelativePath(folderPath, pathInfo.FullPath);
                Directory.CreateDirectory(Path.Combine(newFolderPath, relativePath));
            }

            foreach (PathInfo pathInfo in EnumeratePathInfoEntries(folderPath, "*", true))
            {
                if (pathInfo.IsDirectory || Path.GetExtension(pathInfo.FullPath) == AssetMeta.MetaExtension)
                    continue;
                string relativePath = Path.GetRelativePath(folderPath, pathInfo.FullPath);
                string newAssetPath = Path.Combine(newFolderPath, relativePath);
                CopyAsset(pathInfo.FullPath, newAssetPath);
            }

            return true;
        }

        public static bool MoveFolder(string oldFolderPath, string newFolderPath)
        {
            if (!Path.Exists(oldFolderPath) || Path.Exists(newFolderPath) || string.IsNullOrEmpty(newFolderPath))
                return false;

            Directory.CreateDirectory(newFolderPath);

            var folderEntries = EnumeratePathInfoEntries(oldFolderPath, "*", true).ToList();
            foreach (PathInfo pathInfo in folderEntries.Where(x => x.IsDirectory))
            {
                string relativeFolderPath = Path.GetRelativePath(oldFolderPath, pathInfo.FullPath);
                string newRelativeFolderPath = Path.Combine(newFolderPath, relativeFolderPath);

                Directory.CreateDirectory(newRelativeFolderPath);
            }

            foreach (PathInfo pathInfo in folderEntries.Where(x => !x.IsDirectory))
            {
                if (!IsSupportedAssetFile(pathInfo.FullPath))
                    continue;

                string relativeAssetPath = Path.GetRelativePath(oldFolderPath, pathInfo.FullPath);
                string newAssetPath = Path.Combine(newFolderPath, relativeAssetPath);
                MoveAsset(pathInfo.FullPath, newAssetPath);
            }

            foreach (PathInfo pathInfo in folderEntries.Where(x => x.IsDirectory))
            {
                if (Directory.GetFiles(pathInfo.FullPath).Length == 0)
                {
                    Directory.Delete(pathInfo.FullPath);
                }
                string relativeFolderPath = Path.GetRelativePath(oldFolderPath, pathInfo.FullPath);
                string newRelativeFolderPath = Path.Combine(newFolderPath, relativeFolderPath);

                Directory.CreateDirectory(newRelativeFolderPath);
            }

            DeleteEmptyFolders(oldFolderPath);
            return true;
        }

        public static bool RenameFolder(string folderPath, string newFolderName)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);

            string newFolderPath = Path.Combine(directoryInfo.Parent?.FullName ?? "", newFolderName);
            return MoveFolder(folderPath, newFolderPath);
        }

        public static bool DeleteFolder(string folderPath)
        {
            if (!Path.Exists(folderPath))
                return false;

            foreach (PathInfo pathInfo in EnumeratePathInfoEntries(folderPath, "*", true))
            {
                if (pathInfo.IsDirectory || !IsSupportedAssetFile(pathInfo.FullPath))
                    continue;

                if (!DeleteAsset(pathInfo.FullPath))
                    return false;
            }

            Directory.Delete(folderPath, true);
            return true;
        }

        private static void DeleteEmptyFolders(string startFolderPath)
        {
            foreach (string folderPath in Directory.EnumerateDirectories(startFolderPath))
                DeleteEmptyFolders(folderPath);

            if (!Directory.EnumerateFileSystemEntries(startFolderPath).Any())
                Directory.Delete(startFolderPath, false);
        }

        #endregion FolderOperations

        #region Helpers

        public static bool IsSupportedAssetFile(string fullPath)
        {
            return assetImporters.ContainsKey(Path.GetExtension(fullPath));
        }

        public static string GetContentAssetPath(string fullPath)
        {
            return Path.GetRelativePath(ContentFolderPath, fullPath);
        }

        #endregion Helpers

    }
}