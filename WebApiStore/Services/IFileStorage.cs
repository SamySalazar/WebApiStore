namespace WebApiStore.Services
{
    public interface IFileStorage
    {
        public Task<string> Create(byte[] file, string contentType, string extension, string container, string name);
        public Task Delete(string route, string container);
    }
}
