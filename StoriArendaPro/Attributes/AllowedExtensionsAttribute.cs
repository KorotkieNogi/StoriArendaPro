using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Collections.Generic;


namespace StoriArendaPro.Attributes
{
    public class AllowedExtensionsAttribute : ValidationAttribute
    {
        private readonly string[] _extensions;

        public AllowedExtensionsAttribute(string[] extensions)
        {
            _extensions = extensions;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                var extension = Path.GetExtension(file.FileName);
                if (!_extensions.Contains(extension.ToLower()))
                {
                    return new ValidationResult($"Разрешены только: {string.Join(", ", _extensions)}");
                }
            }
            else if (value is List<IFormFile> files)
            {
                foreach (var f in files)
                {
                    var extension = Path.GetExtension(f.FileName);
                    if (!_extensions.Contains(extension.ToLower()))
                    {
                        return new ValidationResult($"Разрешены только: {string.Join(", ", _extensions)}");
                    }
                }
            }

            return ValidationResult.Success;
        }
    }
}
