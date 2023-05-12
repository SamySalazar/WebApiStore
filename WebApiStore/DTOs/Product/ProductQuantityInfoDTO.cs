using System.ComponentModel.DataAnnotations;

namespace WebApiStore.DTOs.Product
{
    public class ProductQuantityInfoDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public string Image { get; set; }
        public int Quantity { get; set; }
    }
}
