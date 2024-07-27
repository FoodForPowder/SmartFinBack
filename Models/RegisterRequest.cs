using System.ComponentModel.DataAnnotations;

namespace SmartFin.Models.RegisterRequest
{
    public class RegisterRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string UserName { get; set; }
        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; }
        [Phone]
        public string PhoneNumber { get; set; }

        [Required]

        [StringLength(50, MinimumLength = 1)]
        public string Name { get; set; }


        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string Password { get; set; }

        [Compare("Password")]
        public string PasswordConfirmation { get; set; }




    }
}