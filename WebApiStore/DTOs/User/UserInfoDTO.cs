using System.ComponentModel.DataAnnotations;

namespace WebApiStore.DTOs.User
{
    public class UserInfoDTO
    {
        public string UserName { get; set; }
        public string Email { get; set; }
    }
}
