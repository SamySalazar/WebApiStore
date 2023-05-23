using AutoMapper;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using MimeKit;
using MimeKit.Text;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Net.WebSockets;
using WebApiStore.DTOs.Email;
using WebApiStore.DTOs.Order;
using WebApiStore.DTOs.Product;
using WebApiStore.Entities;
using WebApiStore.Migrations;
using WebApiStore.Services;
using WebApiStore.Services.EmailService;

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
        private readonly IEmailService emailService;

        public OrdersController(ApplicationDbContext dbContext,
            IMapper mapper,
            UserManager<IdentityUser> userManager,
            IEmailService emailService)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
            this.userManager = userManager;
            this.emailService = emailService;
        }

        // Only Admin
        // Historial de pedidos de todos los usuarios
        [HttpGet("GetOrders")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<List<OrderInfoDTO>>> GetOrders()
        {
            var orders = await dbContext.Orders
                .Include(x => x.User)
                .Include(x => x.OrdersProducts)
                .ThenInclude(x => x.Product)
                .Where(x => !x.ShoppingCart).ToListAsync();
            return mapper.Map<List<OrderInfoDTO>>(orders);
        }

        // Historial de pedidos del usuario
        [HttpGet("GetOrdersByUser")]
        public async Task<ActionResult<List<OrderInfoDTO>>> GetOrdersByUser()
        {
            var userNameClaim = HttpContext.User.Claims.Where(claim => claim.Type == "userName").FirstOrDefault();
            var userName = userNameClaim.Value;
            var user = await userManager.FindByNameAsync(userName);
            var userId = user.Id;

            var orders = await dbContext.Orders
                .Include(x => x.OrdersProducts)
                .ThenInclude(x => x.Product)
                .Where(x => x.UserId == userId && !x.ShoppingCart).ToListAsync();
            return mapper.Map<List<OrderInfoDTO>>(orders);
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
                .FirstOrDefaultAsync(x => x.UserId == userId && x.ShoppingCart);
            if (order == null)
            {
                return BadRequest("No tiene un carrito de compras activo");
            }

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

            // Validación de stock
            if (product.Stock < orderDTO.Quantity)
            {
                return BadRequest("No hay suficiente stock para este pedido");
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

        // Confirmar compra
        [HttpPut("ConfirmOrder")]
        public async Task<ActionResult> PutConfirmOrder([FromForm] ConfirmOrderDTO confirmOrderDTO)
        {
            var userNameClaim = HttpContext.User.Claims.Where(claim => claim.Type == "userName").FirstOrDefault();
            var userName = userNameClaim.Value;
            var user = await userManager.FindByNameAsync(userName);
            var userId = user.Id;

            var order = await dbContext.Orders
                .Include(x => x.OrdersProducts)
                .FirstOrDefaultAsync(x => x.UserId == userId && x.ShoppingCart);
            if (order == null)
            {
                return BadRequest("No tiene un carrito de compras activo");
            }

            mapper.Map(confirmOrderDTO, order);
            order.ShoppingCart = false;
            order.Status = "en proceso";

            // Restar productos del stock
            foreach (var orderProduct in order.OrdersProducts)
            {
                var product = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == orderProduct.ProductId);
                product.Stock -= orderProduct.Quantity;
                dbContext.Entry(product).State = EntityState.Modified;
            }
            
            dbContext.Entry(order).State = EntityState.Modified;
            await dbContext.SaveChangesAsync();
            return NoContent();
        }

        // Only Admin
        // Update Status
        [HttpPut("UpdateStatus/{orderId:int}")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult> PutStatus(int orderId)
        {
            var order = await dbContext.Orders
                .Include(x => x.User)
                .Include(x => x.OrdersProducts)
                .FirstOrDefaultAsync(x => x.Id == orderId && !x.ShoppingCart);
            if (order == null)
            {
                return BadRequest("El pedido seleccionado no existe");
            }

            if (order.Status == "entregada")
            {
                return BadRequest("No se actualizó el estatus, porque el pedido ya fue entregado");
            }

            order.Status = order.Status == "en proceso" ? "en ruta" : "entregada";


            EmailDTO request = new EmailDTO();
            request.Subject = $"Estatus de Pedido #{order.Id} | High Tech";
            request.Body = $"<h1>Orden #{order.Id} {order.Status}</h1><br>" +
                           $"<h3>Hola {order.User}, te adjuntamos los datos de tu compra.</h3>" +
                           "<b>Detalles del pedido:</b><br>" +
                           $"<ul><li><b>No. Pedido:</b> {order.Id}</li><li><b>Estado:</b> {order.Status}</li><li><b>Total:</b> ${order.Total}</li></ul>" +
                           "<br>¿Tiene preguntas o comentarios? Puede ponerse en contacto con nosotros en cualquier momento en +52(81)19877896, o htech.soporte1@gmail.com" +
                           "<br><br><br>Atte.<br><b>High Tech</b>" +
                           "<br><br><br>© 2023 High Tech. Todos los derechos reservados.";
            request.To = order.User.Email;

            emailService.SendEmail(request);

            dbContext.Entry(order).State = EntityState.Modified;
            await dbContext.SaveChangesAsync();
            return Ok($"El estatus de pedido fue cambiado a {order.Status}");
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
