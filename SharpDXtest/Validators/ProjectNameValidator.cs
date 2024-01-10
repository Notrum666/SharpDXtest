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
    public class ProjectNameValidator : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string projectName = value as string;
            if (string.IsNullOrWhiteSpace(projectName))
                return new ValidationResult(false, "Project name cannot be empty");
            if (projectName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
                return new ValidationResult(false, "Project name contains invalid character(s)");

            return ValidationResult.ValidResult;
        }
    }
}
