using System.ComponentModel.DataAnnotations;

namespace SmartFin.Models.LoginRequest
{

    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}