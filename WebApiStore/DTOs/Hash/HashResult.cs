using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WebApiStore.DTOs.Hash
{
    public class HashResult
    {
        public string Hash { get; set; }
        public byte[] Sal { get; set; }
    }
}
