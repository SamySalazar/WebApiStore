using Microsoft.AspNetCore.Identity;
using WebApiStore.DTOs.Product;
using WebApiStore.Entities;

namespace WebApiStore.DTOs.Order
{
    public class OrderShoppingCartInfoDTO
    {
        public int Id { get; set; }
        public decimal Total { get; set; }        
        public List<ProductQuantityInfoDTO> Products { get; set; }
    }
}
