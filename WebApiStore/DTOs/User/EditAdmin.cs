using System.ComponentModel.DataAnnotations;

namespace WebApiStore.DTOs.User
{
    public class EditAdmin
    {
        [Required]
        public string UserName { get; set; }
    }
}
