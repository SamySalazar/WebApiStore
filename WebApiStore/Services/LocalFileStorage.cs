using Microsoft.AspNetCore.Razor.Hosting;

namespace WebApiStore.Services
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IHttpContextAccessor httpContextAccessor;

        public LocalFileStorage(IWebHostEnvironment webHostEnvironment,
            IHttpContextAccessor httpContextAccessor)
        {
            this.webHostEnvironment = webHostEnvironment;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> Create(byte[] file, string contentType, string extension, string container, string name)
        {
            string wwwrootPath = webHostEnvironment.WebRootPath;
            if (string.IsNullOrEmpty(wwwrootPath))
            {
                throw new Exception();
            }

            string fileFolder = Path.Combine(wwwrootPath, container);
            if (Directory.Exists(fileFolder))
            {
                Directory.CreateDirectory(fileFolder);
            }

            string newName = $"{name}{extension}";
            string newRoute = Path.Combine(fileFolder, newName);

            await File.WriteAllBytesAsync(newRoute, file);

            string url = $"{httpContextAccessor.HttpContext.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host}";
            string dbUrl = Path.Combine(url, container, newName).Replace("\\", "/");

            return dbUrl;
        }

        public Task Delete(string route, string container)
        {
            string wwwrootPath = webHostEnvironment.WebRootPath;
            if (string.IsNullOrEmpty(wwwrootPath))
            {
                throw new Exception();
            }

            var fileName = Path.GetFileName(route);

            string finalPath= Path.Combine(wwwrootPath, container, fileName);
            if (File.Exists(finalPath))
            {
                File.Delete(finalPath);
            }
            return Task.CompletedTask;
        }
    }
}
