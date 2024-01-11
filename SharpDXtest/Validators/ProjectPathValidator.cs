using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Editor
{
    public class ProjectPathValidator : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string parentFolderPath = value as string;

            if (string.IsNullOrWhiteSpace(parentFolderPath))
                return new ValidationResult(false, "Project path cannot be empty");
            if (parentFolderPath.IndexOfAny(Path.GetInvalidPathChars()) != -1)
                return new ValidationResult(false, "Project path contains invalid character(s)");
            if (!IsValidPath(parentFolderPath))
                return new ValidationResult(false, "Project path is invalid");

            return ValidationResult.ValidResult;
        }

        private bool IsValidPath(string path)
        {
            // Check if the path is rooted in a driver
            if (path.Length < 3)
                return false;

            Regex driveCheck = new Regex(@"^[a-zA-Z]:\\$");
            if (!driveCheck.IsMatch(path.Substring(0, 3)))
                return false;

            // Check if such driver exists
            IEnumerable<string> allMachineDrivers = DriveInfo.GetDrives().Select(drive => drive.Name);
            if (!allMachineDrivers.Contains(path.Substring(0, 3)))
                return false;

            // Check if the rest of the path is valid
            string InvalidFileNameChars = new string(Path.GetInvalidPathChars());
            InvalidFileNameChars += @":/?*" + "\"";
            Regex containsABadCharacter = new Regex("[" + Regex.Escape(InvalidFileNameChars) + "]");
            if (containsABadCharacter.IsMatch(path.Substring(3, path.Length - 3)))
                return false;
            if (path[path.Length - 1] == '.')
                return false;

            return true;
        }
    }
}
