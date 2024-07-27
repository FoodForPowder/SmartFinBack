using System.ComponentModel.DataAnnotations;

namespace SmartFin.Models.RevokeRequest
{

    public class RevokeRequest
    {
        [Required]
        public string RefreshToken { get; set; }
    }
}