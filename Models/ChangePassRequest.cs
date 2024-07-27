using System.ComponentModel.DataAnnotations;

namespace SmartFin.Models
{

    public class ChangePassRequest
    {
        [Required]
        public string userId { get; set; }
        [Required]
        public string oldPassword { get; set; }
        [Required]

        public string newPassword { get; set; }




    }
}