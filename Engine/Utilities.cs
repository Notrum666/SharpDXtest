using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Text;
using System.Text.RegularExpressions;

namespace Engine
{
    public static partial class FileSystemHelper
    {
        private static readonly EnumerationOptions defaultEnumerator = new EnumerationOptions() { IgnoreInaccessible = true };
        private static readonly EnumerationOptions recursiveEnumerator = new EnumerationOptions() { IgnoreInaccessible = true, RecurseSubdirectories = true };

        public static string SanitizeFileName(string name)
        {
            int fileNameIndex = name.LastIndexOf(Path.DirectorySeparatorChar) + 1;
            StringBuilder path = new StringBuilder(name[..fileNameIndex]);
            StringBuilder fileName = new StringBuilder(name[fileNameIndex..]);

            foreach (char c in Path.GetInvalidPathChars())
            {
                path.Replace(c, '_');
            }
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName.Replace(c, '_');
            }

            return path.Append(fileName).ToString();
        }

        public static string GenerateUniquePath(string path)
        {
            if (!Path.Exists(path))
                return path;

            path = Path.TrimEndingDirectorySeparator(path);

            FileInfo fileInfo = new FileInfo(path);
            string parentFolderName = fileInfo.DirectoryName ?? string.Empty;
            string fileName = fileInfo.Name;
            string extension = fileInfo.Extension;

            Match regexMatch = FileNameIndexRegex().Match(fileName);
            int fileNameIndex = regexMatch.Success ? int.Parse(regexMatch.Value) : 0;
            fileName = fileName[..^regexMatch.Length];

            do
            {
                fileNameIndex++;
                path = Path.Combine(parentFolderName, $"{fileName} {fileNameIndex}{extension}");
            } while (Path.Exists(path));

            return path;
        }

        public static IEnumerable<PathInfo> EnumeratePathInfoEntries(string path, string expression, bool recursive)
        {
            return new FileSystemEnumerable<PathInfo>(path, PathInfo.FromSystemEntry, recursive ? recursiveEnumerator : defaultEnumerator)
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) => FileSystemName.MatchesSimpleExpression(expression.AsSpan(), entry.FileName)
            };
        }

        public struct PathInfo
        {
            public bool IsDirectory;
            public string FullPath;

            public static PathInfo FromSystemEntry(ref FileSystemEntry entry)
            {
                return new PathInfo
                {
                    IsDirectory = entry.IsDirectory,
                    FullPath = entry.ToFullPath()
                };
            }
        }

        [GeneratedRegex("\\s\\d+$", RegexOptions.RightToLeft)]
        private static partial Regex FileNameIndexRegex();
    }
}