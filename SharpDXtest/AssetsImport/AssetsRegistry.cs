using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Editor.AssetsImport;

using Engine;

using static Engine.FileSystemHelper;

namespace Editor
{
    public static class AssetsRegistry
    {
        public const string ContentFolderName = "Content";
        public static string ContentFolderPath { get; private set; }

        private static readonly Dictionary<string, AssetImporter> assetImporters = new Dictionary<string, AssetImporter>();

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
            ImportFolder(ContentFolderPath);
        }

        #region AssetOperations //Import, Copy, Move, Rename, Delete

        public static Guid? ImportAsset(string assetPath)
        {
            if (!Path.Exists(assetPath))
                return null;

            string assetExtension = Path.GetExtension(assetPath);
            if (!assetImporters.TryGetValue(assetExtension, out AssetImporter importer))
                return null;

            return importer.ImportAsset(assetPath);
        }

        public static bool CopyAsset(string assetPath, string newAssetPath)
        {
            if (!Path.Exists(assetPath))
                return false;

            newAssetPath = GenerateUniquePath(newAssetPath);
            File.Copy(assetPath, newAssetPath);
            return ImportAsset(newAssetPath) != null;
        }

        public static bool MoveAsset(string oldAssetPath, string newAssetPath)
        {
            if (!Path.Exists(oldAssetPath) || Path.Exists(newAssetPath))
                return false;

            string oldContentAssetPath = GetContentAssetPath(oldAssetPath);
            string newContentAssetPath = GetContentAssetPath(newAssetPath);

            if (!IsSupportedAssetFile(oldAssetPath) || !AssetsManager.UpdateAssetPath(oldContentAssetPath, newContentAssetPath))
                return false;

            File.Move(oldAssetPath, newAssetPath);
            return true;
        }

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

            string assetExtension = Path.GetExtension(assetPath);
            string metaPath = Path.ChangeExtension(assetPath, $"{assetExtension}{AssetMeta.MetaExtension}");
            File.Delete(metaPath);

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
            if (!Path.Exists(oldFolderPath) || Path.Exists(newFolderPath))
                return false;

            foreach (PathInfo pathInfo in EnumeratePathInfoEntries(oldFolderPath, "*", true))
            {
                if (pathInfo.IsDirectory || !IsSupportedAssetFile(pathInfo.FullPath))
                    continue;

                string relativeAssetPath = Path.GetRelativePath(oldFolderPath, pathInfo.FullPath);
                string newAssetPath = Path.Combine(newFolderPath, relativeAssetPath);

                string oldContentAssetPath = GetContentAssetPath(pathInfo.FullPath);
                string newContentAssetPath = GetContentAssetPath(newAssetPath);

                if (!AssetsManager.UpdateAssetPath(oldContentAssetPath, newContentAssetPath))
                    return false;
            }

            Directory.Move(oldFolderPath, newFolderPath);
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

                string contentRelativePath = GetContentAssetPath(pathInfo.FullPath);
                if (!AssetsManager.DeleteAsset(contentRelativePath))
                    return false;
            }

            Directory.Delete(folderPath, true);
            return true;
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