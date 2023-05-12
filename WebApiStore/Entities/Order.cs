using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WebApiStore.Entities
{
    public class Order
    {
        [Required]
        public int Id { get; set; }
        public bool ShoppingCart { get; set; }
        public string Address { get; set; }
        public string Status { get; set; }
        public string PaymentMethod { get; set; }
        public decimal Total { get; set; }
        public DateTime Date { get; set; }        
        public List<OrderProduct> OrdersProducts { get; set; }
        public string UserId { get; set; }
        public IdentityUser User { get; set; }
    }
}
