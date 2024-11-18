using System.ComponentModel.DataAnnotations;

namespace SmartFin.DTOs.Category
{

    public class CategoryDTO
    {
        
        public int id { get; set; }
        public string name { get; set; }
        public int UserId { get; set; }
    }
}