using System.ComponentModel.DataAnnotations;

namespace SmartFin.DTOs.User{
    public class UpdateUserDto{

        [StringLength(50)]
        public string Name { get; set;}

        
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set;}

        [Phone]
        public string PhoneNumber { get; set;}


    }
}