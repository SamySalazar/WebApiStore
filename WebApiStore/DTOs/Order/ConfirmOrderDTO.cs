using System.ComponentModel.DataAnnotations;
using WebApiStore.Validations;

namespace WebApiStore.DTOs.Order
{
    public class ConfirmOrderDTO
    {
        [Required]
        public string Address { get; set; }
        [Required]
        [Category(new[] { "Debito", "Credito", "Efectivo" })]
        public string PaymentMethod { get; set; }
    }
}
