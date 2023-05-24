using AutoMapper;
using Azure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using WebApiStore.DTOs.Product;
using WebApiStore.Entities;
using WebApiStore.Filters;
using WebApiStore.Services;

namespace WebApiStore.Controllers
{
    [ApiController]
    [Route("api/products")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext dbContext;
        private readonly IMapper mapper;
        private readonly IFileStorage fileStorage;
        private readonly UserManager<IdentityUser> userManager;
        private readonly ILogger<ProductsController> logger;

        public ProductsController(ApplicationDbContext dbContext, 
            IMapper mapper,
            IFileStorage fileStorage,
            UserManager<IdentityUser> userManager,
            ILogger<ProductsController> logger)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
            this.fileStorage = fileStorage;
            this.userManager = userManager;
            this.logger = logger;
        }      

        [HttpGet]
        [ServiceFilter(typeof(ActionFilter))]
        public async Task<ActionResult<List<ProductInfoDTO>>> GetList()
        {
            // throw new NotImplementedException();
            var products = await dbContext.Products.ToListAsync();
            return mapper.Map<List<ProductInfoDTO>>(products);
        }

        [HttpGet("{id:int}", Name = "GetProductById")]
        public async Task<ActionResult<ProductInfoDTO>> GetById(int id)
        {
            var product = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == id);

            if (product == null)
            {
                return NotFound();
            }
            return mapper.Map<ProductInfoDTO>(product);
        }

        [HttpGet("SearchProduct")]
        public async Task<ActionResult<List<ProductInfoDTO>>> GetSearchProduct([FromQuery] string name, [FromQuery] string category)
        {
            var products = await dbContext.Products
                .Where(x => (x.Name.Contains(name) || string.IsNullOrEmpty(name)) &&
                            (x.Category == category || string.IsNullOrEmpty(category)))
                .ToListAsync();
            return mapper.Map<List<ProductInfoDTO>>(products);
        }

        [HttpGet("Recommendations")]
        public async Task<ActionResult<List<ProductInfoDTO>>> GetRecommendations()
        {
            var userNameClaim = HttpContext.User.Claims.Where(claim => claim.Type == "userName").FirstOrDefault();
            var userName = userNameClaim.Value;
            var user = await userManager.FindByNameAsync(userName);
            var userId = user.Id;

            var maxCategory = dbContext.Products                
                .Join(dbContext.OrdersProducts, p => p.Id, op => op.ProductId, (p, op) => new { Product = p, OrderProduct = op })
                .Join(dbContext.Orders, j => j.OrderProduct.OrderId, o => o.Id, (j, o) => new { j.Product, j.OrderProduct, Order = o })
                .Where(j => j.Order.UserId == userId)
                .GroupBy(j => new { j.Product.Category, j.Order.UserId })
                .Select(g => new { Category = g.Key.Category, UserId = g.Key.UserId, Quantity = g.Sum(p => p.OrderProduct.Quantity) })
                .OrderByDescending(g => g.Quantity)
                .FirstOrDefault();

            Console.WriteLine("Category: " + maxCategory?.Category);
            Console.WriteLine("UserId: " + maxCategory?.UserId);
            Console.WriteLine("Quantity: " + maxCategory?.Quantity);

            var products = await dbContext.Products
                .Where(x => x.Category == maxCategory.Category)
                .ToListAsync();

            return mapper.Map<List<ProductInfoDTO>>(products);
        }

        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult> Post([FromForm] ProductDTO productDTO)
        {
            var product = mapper.Map<Product>(productDTO);

            if(productDTO.Image != null)
            {
                string imageUrl = await saveImage(productDTO.Image);
                product.Image = imageUrl;
            }

            dbContext.Add(product);
            await dbContext.SaveChangesAsync();

            var infoDTO = mapper.Map<ProductInfoDTO>(product);
            return new CreatedAtRouteResult("GetProductById", new { id = product.Id }, infoDTO);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult> Put(int id, [FromForm] ProductDTO productDTO)
        {
            var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            mapper.Map(productDTO, product);

            if (productDTO.Image != null)
            {
                if (!string.IsNullOrEmpty(product.Image))
                {
                    await fileStorage.Delete(product.Image, "ProductsImage");
                }
                string imageUrl = await saveImage(productDTO.Image);
                product.Image = imageUrl;
            }

            dbContext.Entry(product).State = EntityState.Modified;
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Se modificó correctamente");
            return NoContent();
        }

        [HttpPut("Stock/{id:int}")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult> PutStock(int id, [FromForm] int stock)
        {
            var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            product.Stock += stock;

            dbContext.Entry(product).State = EntityState.Modified;
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Se modificó correctamente");
            return NoContent();
        }

        [HttpPatch("patch/{id:int}")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult> Patch(int id, JsonPatchDocument<ProductPatchDTO> patchDocument)
        {
            if (patchDocument == null)
            {
                return BadRequest();
            }

            var productDB = await dbContext.Products.FirstOrDefaultAsync(
                x => x.Id == id);
            if (productDB == null)
            {
                return NotFound();
            }

            var productPatchDTO = mapper.Map<ProductPatchDTO>(productDB);

            patchDocument.ApplyTo(productPatchDTO, ModelState);

            var esValido = TryValidateModel(productPatchDTO);
            if (!esValido)
            {
                return BadRequest(ModelState);
            }

            mapper.Map(productPatchDTO, productDB);

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Se modificó correctamente");
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult> Delete(int id)
        {
            var exist = await dbContext.Products.AnyAsync(x => x.Id == id);
            if (!exist)
            {
                return NotFound();
            }

            dbContext.Remove(new Product()
            {
                Id = id
            });
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Se eliminó correctamente");
            return NoContent();
        }

        // Método para guardar imagen en memoria
        private async Task<string> saveImage(IFormFile image)
        {
            using var stream = new MemoryStream();

            await image.CopyToAsync(stream);

            var fileBytes = stream.ToArray();

            return await fileStorage
                .Create(fileBytes, image.ContentType, Path.GetExtension(image.FileName), "ProductsImage", Guid.NewGuid().ToString());
        }
    }
}
