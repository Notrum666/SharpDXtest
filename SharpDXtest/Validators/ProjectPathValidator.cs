using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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

            return ValidationResult.ValidResult;
        }
    }
}
