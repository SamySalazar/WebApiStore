using WebApiStore.DTOs.Product;
using WebApiStore.DTOs.User;

namespace WebApiStore.DTOs.Order
{
    public class OrderInfoDTO
    {
        public int Id { get; set; }
        public UserInfoDTO User { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; }
        public string Address { get; set; }
        public List<ProductQuantityInfoDTO> Products { get; set; }
    }
}
