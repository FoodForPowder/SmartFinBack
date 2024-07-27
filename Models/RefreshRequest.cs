using System.ComponentModel.DataAnnotations;

namespace SmartFin.Models.RefreshRequest
{
    public class RefreshRequest
    {
        [Required]
        public string AccessToken { get; set; }
        [Required]
        public string RefreshToken { get; set; }
    }
}