using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SmartFin.Entities
{
    public class Transaction
    {
        [Key]
        public int id { get; set; }

        [Required]
        public decimal sum { get; set; }

        public DateTime Date { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        [JsonIgnore]
        public virtual User? user { get; set; }

        public int? CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        [JsonIgnore]
        public virtual Category? category { get; set; }
    }
}