using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.VisualBasic;
using System.Security.Cryptography;
using WebApiStore.DTOs.Hash;

namespace WebApiStore.Services
{
    public class HashService
    {
        public HashResult Hash(string plainText)
        {
            var sal = new byte[16];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(sal);
            }
            return Hash(plainText, sal);
        }

        public HashResult Hash(string plainText, byte[] sal)
        {
            var key = KeyDerivation.Pbkdf2(password: plainText,
                salt: sal,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10000,
                numBytesRequested: 32);

            var hash = Convert.ToBase64String(key);
            return new HashResult
            {
                Hash = hash,
                Sal = sal
            };
        }
    }
}
