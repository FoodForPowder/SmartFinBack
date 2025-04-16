using System.ComponentModel.DataAnnotations;

namespace SmartFin.DTOs.Category
{

    public class CategoryDTO
    {

        [Required]
        public string name { get; set; }
    }
}