using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StoriArendaPro.Attributes
{
    public class MaxFileSizeAttribute : ValidationAttribute
    {
        private readonly int _maxFileSize;

        public MaxFileSizeAttribute(int maxFileSize)
        {
            _maxFileSize = maxFileSize;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                if (file.Length > _maxFileSize)
                {
                    return new ValidationResult($"Максимальный размер файла: {_maxFileSize / 1024 / 1024}MB");
                }
            }
            else if (value is List<IFormFile> files)
            {
                foreach (var f in files)
                {
                    if (f.Length > _maxFileSize)
                    {
                        return new ValidationResult($"Максимальный размер файла: {_maxFileSize / 1024 / 1024}MB");
                    }
                }
            }

            return ValidationResult.Success;
        }
    }
}
