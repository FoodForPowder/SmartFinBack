using System.ComponentModel.DataAnnotations;

namespace SmartFin.DTOs.Category
{

    public class UpdateCategoryDTO
    {

        [Required]
        public string name { get; set; }
        [Required]
        public int UserId { get; set; }
    }
}