using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace WebApiStore.Validations
{
    public class FileExtensionAttribute : ValidationAttribute
    {
        private readonly string[] validTypes;

        public FileExtensionAttribute(string[] validTypes)
        {
            this.validTypes = validTypes;
        }
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var formFile = value as IFormFile;
            if (formFile != null)
            {                
                if (!validTypes.Contains(formFile.ContentType))
                {
                    return new ValidationResult($"Los tipos validos son {string.Join(",", validTypes)}");
                }
            }
            return ValidationResult.Success;
        }
    }
}
