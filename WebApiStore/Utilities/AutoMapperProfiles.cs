using AutoMapper;
using Microsoft.Identity.Client;
using WebApiStore.DTOs.Product;
using WebApiStore.Entities;

namespace WebApiStore.Utilities
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            // ------------- Products -------------
            CreateMap<Product, ProductInfoDTO>()
                .ReverseMap();

            CreateMap<ProductDTO, Product>()
                .ForMember(m => m.Image, options => options.Ignore());
        }
    }
}
