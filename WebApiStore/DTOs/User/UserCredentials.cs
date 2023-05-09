using System.ComponentModel.DataAnnotations;

namespace WebApiStore.DTOs.User
{
    public class UserCredentials
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
