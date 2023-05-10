using System.ComponentModel.DataAnnotations;

namespace WebApiStore.DTOs.Product
{
    public class ProductDTO
    {
        [Required]
        [StringLength(256)]
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public IFormFile Image { get; set; }        
    }
}
