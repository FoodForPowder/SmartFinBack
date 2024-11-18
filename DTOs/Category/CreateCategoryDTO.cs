using System.ComponentModel.DataAnnotations;

namespace SmartFin.DTOs.Category
{

    public class CreateCategoryDTO
    {

        [Required]
        public string name { get; set; }
        [Required]
        public int UserId { get; set; }
    }
}