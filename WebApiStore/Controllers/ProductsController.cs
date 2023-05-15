using AutoMapper;
using Azure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using WebApiStore.DTOs.Product;
using WebApiStore.Entities;
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

        public ProductsController(ApplicationDbContext dbContext, 
            IMapper mapper,
            IFileStorage fileStorage)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
            this.fileStorage = fileStorage;
        }

        [HttpGet]
        public async Task<ActionResult<List<ProductInfoDTO>>> GetList()
        {
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
