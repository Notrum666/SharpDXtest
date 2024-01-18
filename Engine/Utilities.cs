using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Engine
{
    public static partial class FileSystemHelper
    {
        private static readonly EnumerationOptions defaultEnumerator = new EnumerationOptions() { IgnoreInaccessible = true };
        private static readonly EnumerationOptions recursiveEnumerator = new EnumerationOptions() { IgnoreInaccessible = true, RecurseSubdirectories = true };

        public static string SanitizeFileName(string name, bool sanitizeDots = false)
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

            if (sanitizeDots)
                fileName.Replace('.', '_');

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

    public ref struct Ranged<T> where T : struct, INumber<T>
    {
        private readonly ref T value;

        private readonly T? min;
        private readonly T? max;

        private readonly Action onSet;
        private readonly string propertyName;

        /// <summary>
        /// Equality comparison is strict: value CAN be equal to min or max
        /// </summary>
        public Ranged(ref T value, T? min = null, T? max = null, Action onSet = null, [CallerMemberName] string propertyName = "")
        {
            this.value = ref value;
            this.min = min;
            this.max = max;
            this.onSet = onSet;
            this.propertyName = propertyName;
        }

        public void Set(T newValue)
        {
            if (newValue < min)
            {
                Logger.Log(LogType.Error, $"{propertyName} can't be less than {min}, but tried to set = {newValue}");
                return;
            }

            if (newValue > max)
            {
                Logger.Log(LogType.Error, $"{propertyName} can't be greater than {max}, but tried to set = {newValue}");
                return;
            }

            value = newValue;
            onSet?.Invoke();
        }

        public static implicit operator T(Ranged<T> r) => r.value;

        public override string ToString()
        {
            return value.ToString();
        }
    }
}