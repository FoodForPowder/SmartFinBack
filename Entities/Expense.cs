using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartFin.Entities
{
    public class Expense
    {
        [Key]
        public int id { get; set; }
        [Required]

        public decimal sum { get; set; }

        public DateTime Date { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? user { get; set; } = null;

        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public virtual Category? category { get; set; } = null;
    }
}