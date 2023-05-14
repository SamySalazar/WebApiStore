using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.Data;
using System.Net.WebSockets;
using WebApiStore.DTOs.Order;
using WebApiStore.DTOs.Product;
using WebApiStore.Entities;
using WebApiStore.Migrations;

namespace WebApiStore.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext dbContext;
        private readonly IMapper mapper;
        private readonly UserManager<IdentityUser> userManager;

        public OrdersController(ApplicationDbContext dbContext,
            IMapper mapper,
            UserManager<IdentityUser> userManager)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
            this.userManager = userManager;
        }

        // Consultar carrito
        [HttpGet("GetShoppingCart")]
        public async Task<ActionResult<OrderShoppingCartInfoDTO>> GetShoppingCart()
        {
            var userNameClaim = HttpContext.User.Claims.Where(claim => claim.Type == "userName").FirstOrDefault();
            var userName = userNameClaim.Value;
            var user = await userManager.FindByNameAsync(userName);
            var userId = user.Id;

            var order = await dbContext.Orders
                .Include(x => x.OrdersProducts)
                .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.UserId == userId);

            return mapper.Map<OrderShoppingCartInfoDTO>(order);
        }

        // Agregar al carrito
        [HttpPost("AddToShoppingCart")]        
        public async Task<ActionResult> Post([FromForm] OrderDTO orderDTO)
        {
            var product = await dbContext.Products.FirstOrDefaultAsync(
                productDB => productDB.Id == orderDTO.ProductId);
            if (product == null)
            {
                return BadRequest("No existe el producto requerido");
            }

            // Obtener datos de usuario
            var userNameClaim = HttpContext.User.Claims.Where(claim => claim.Type == "userName").FirstOrDefault();
            var userName = userNameClaim.Value;
            var user = await userManager.FindByNameAsync(userName);
            var userId = user.Id;

            var productPrice = product.Price;            

            // Validar si ya tiene una carrio de compras activo
            var shoppingCart = await dbContext.Orders
                .Include(x => x.OrdersProducts)
                .FirstOrDefaultAsync(x => x.UserId == userId && x.ShoppingCart);
            if (shoppingCart != null)
            {
                // Validar si ya existe el producto selecionado en el carrito de compras
                var exists = shoppingCart.OrdersProducts.Any(x => x.ProductId == orderDTO.ProductId);
                if (exists)
                {
                    var indexProduct = shoppingCart.OrdersProducts.FindIndex(x => x.ProductId == orderDTO.ProductId);
                    shoppingCart.OrdersProducts[indexProduct].Quantity += orderDTO.Quantity;                    
                }
                else
                {
                    var orderProduct = new OrderProduct { ProductId = orderDTO.ProductId, Quantity = orderDTO.Quantity };
                    shoppingCart.OrdersProducts.Add(orderProduct);
                }
                shoppingCart.Total += productPrice * orderDTO.Quantity;
                // Guardar cambios
                dbContext.Entry(shoppingCart).State = EntityState.Modified;
                await dbContext.SaveChangesAsync();

                //var infoDTO1 = mapper.Map<OrderShoppingCartInfoDTO>(shoppingCart);
                //return new CreatedAtRouteResult("AddToShoppingCart", infoDTO1);
                return Ok();
            }

            // Crear un nuevo carrito de compras
            var order = new Order
            {
                Date = DateTime.Now,
                UserId = userId,
                ShoppingCart = true,
                OrdersProducts = new List<OrderProduct>()
            };
            var result = new OrderProduct { ProductId = orderDTO.ProductId, Quantity = orderDTO.Quantity };
            order.OrdersProducts.Add(result);
            order.Total += productPrice * orderDTO.Quantity;
            // Guardar cambios
            dbContext.Add(order);
            await dbContext.SaveChangesAsync();

            //var infoDTO2 = mapper.Map<OrderShoppingCartInfoDTO>(order);
            //return new CreatedAtRouteResult("AddToShoppingCart", infoDTO2);
            return Ok();
        }

        [HttpDelete("CancelShoppingCar")]
        public async Task<ActionResult> DeleteShoppingCar()
        {
            // Obtener datos de usuario
            var userNameClaim = HttpContext.User.Claims.Where(claim => claim.Type == "userName").FirstOrDefault();
            var userName = userNameClaim.Value;
            var user = await userManager.FindByNameAsync(userName);
            var userId = user.Id;

            // Validar si ya tiene una carrio de compras activo
            var shoppingCart = await dbContext.Orders
                .FirstOrDefaultAsync(x => x.UserId == userId && x.ShoppingCart);

            if (shoppingCart == null)
            {
                return NotFound("No hay un carrito de compras activo");
            }

            dbContext.Remove(shoppingCart);
            await dbContext.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("RemoveProductShoppingCar/{productId:int}")]
        public async Task<ActionResult> DeleteProductSC (int productId)
        {
            var product = await dbContext.Products.FirstOrDefaultAsync(
                x => x.Id == productId);
            if (product == null)
            {
                return BadRequest("No existe el producto requerido");
            }

            var productPrice = product.Price;

            // Obtener datos de usuario
            var userNameClaim = HttpContext.User.Claims.Where(claim => claim.Type == "userName").FirstOrDefault();
            var userName = userNameClaim.Value;
            var user = await userManager.FindByNameAsync(userName);
            var userId = user.Id;

            // Validar si ya tiene una carrio de compras activo
            var shoppingCart = await dbContext.Orders
                .Include(x => x.OrdersProducts)
                .FirstOrDefaultAsync(x => x.UserId == userId && x.ShoppingCart);

            if (shoppingCart == null)
            {
                return NotFound("No hay un carrito de compras activo");
            }
            var index = shoppingCart.OrdersProducts.FindIndex(x => x.ProductId == productId);
            var quantity = shoppingCart.OrdersProducts[index].Quantity;

            shoppingCart.Total -= productPrice * quantity;            
            dbContext.Entry(shoppingCart).State = EntityState.Modified;

            dbContext.Remove(shoppingCart.OrdersProducts[index]);
            await dbContext.SaveChangesAsync();
            return Ok();
        }
    }
}
