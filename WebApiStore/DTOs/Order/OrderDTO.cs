using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using WebApiStore.Entities;

namespace WebApiStore.DTOs.Order
{
    public class OrderDTO : IValidatableObject
    {
        [Required]
        public int ProductId { get; set; }
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "El valor debe ser mayor que cero.")]
        public int Quantity { get; set; }

        // Validación por modelo
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Quantity > 0)
            {
                if (Quantity > 10)
                {
                    yield return new ValidationResult("No se puede pedir más de 10 productos de cada tipo",
                        new string[] { nameof(Quantity) });
                }
            }
        }
    }
}
