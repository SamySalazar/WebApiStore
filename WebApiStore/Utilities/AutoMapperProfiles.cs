using AutoMapper;
using Microsoft.Identity.Client;
using WebApiStore.DTOs.Order;
using WebApiStore.DTOs.Product;
using WebApiStore.Entities;
using static System.Net.Mime.MediaTypeNames;

namespace WebApiStore.Utilities
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            // ------------- Products -------------
            CreateMap<Product, ProductInfoDTO>()
                .ReverseMap();

            CreateMap<Product, ProductQuantityInfoDTO>()
                .ReverseMap();

            CreateMap<ProductDTO, Product>()
                .ForMember(m => m.Image, options => options.Ignore());

            // ------------- Orders -------------
            CreateMap<OrderDTO, Order>()
                .ForMember(m => m.OrdersProducts, options => options.MapFrom(MapOrdersProducts));

            CreateMap<Order, OrderShoppingCartInfoDTO>()
                .ForMember(m => m.Products, options => options.MapFrom(MapOrderShoppingCartInfoDTOProduct));
        }

        private List<ProductQuantityInfoDTO> MapOrderShoppingCartInfoDTOProduct(Order order, OrderShoppingCartInfoDTO orderShoppingCartInfoDTO)
        {
            var result = new List<ProductQuantityInfoDTO>();

            if (order.OrdersProducts == null) { return result; }

            foreach (var orderProduct in order.OrdersProducts)
            {
                result.Add(new ProductQuantityInfoDTO()
                {
                    Id = orderProduct.ProductId,
                    Name = orderProduct.Product.Name,
                    Description = orderProduct.Product.Description,
                    Category = orderProduct.Product.Category,
                    Price = orderProduct.Product.Price,
                    Image = orderProduct.Product.Image,
                    Quantity = orderProduct.Quantity
                });
            }

            return result;
        }

        private OrderProduct MapOrdersProducts(OrderDTO orderDTO, Order order)
        {            
            var result = new OrderProduct { ProductId = orderDTO.ProductId };
            return result;
        }
    }
}
