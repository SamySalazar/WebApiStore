using System.ComponentModel.DataAnnotations;
using WebApiStore.Validations;

namespace WebApiStore.DTOs.Product
{
    public class SearchProductDTO
    {
        public string Name { get; set; }

        [Category(new[] { "Computadoras", "Hardware", "Accesorios", "Almacenamiento" })]
        public string Category { get; set; }
    }
}
