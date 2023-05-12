using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using WebApiStore.Entities;

namespace WebApiStore.DTOs.Order
{
    public class OrderDTO
    {
        [Required]
        public int ProductId { get; set; }
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "El valor debe ser mayor que cero.")]
        public int Quantity { get; set; }
    }
}
