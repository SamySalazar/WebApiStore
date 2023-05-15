using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.ComponentModel.DataAnnotations;

namespace WebApiStore.Entities
{
    public class Product
    {
        public int Id { get; set; }
        [Required]
        [StringLength(256)]
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public string Image { get; set; }
        public int Stock { get; set; }
        public List<OrderProduct> OrdersProducts { get; set; }
    }
}
