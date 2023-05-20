using System.ComponentModel.DataAnnotations;
using WebApiStore.Validations;

namespace WebApiStore.DTOs.Product
{
    public class ProductPatchDTO
    {
        [Required]
        [StringLength(256)]
        public string Name { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        [Category(new[] { "Computadoras", "Hardware", "Accesorios", "Almacenamiento" })]
        public string Category { get; set; }
        [Required]
        public decimal Price { get; set; }
        [Required]
        public int Stock { get; set; }
    }
}
