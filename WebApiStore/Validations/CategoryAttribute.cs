using System.ComponentModel.DataAnnotations;

namespace WebApiStore.Validations
{
    public class CategoryAttribute : ValidationAttribute
    {
        private readonly string[] validCategories;

        public CategoryAttribute(string[] validCategories)
        {
            this.validCategories = validCategories;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value != null)
            {
                if (!validCategories.Contains(value))
                {
                    return new ValidationResult($"Las categorías validas son: {string.Join(",", validCategories)}");
                }
            }
            return ValidationResult.Success;
        }
    }
}
