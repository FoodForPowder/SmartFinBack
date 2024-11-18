using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SmartFin.Entities
{
    public class Category
    {
        [Key]
        public int id { get; set; }
        [Required]
        [StringLength(50)]
        public string name { get; set; }
        public int UserId { get; set; }

        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

        [ForeignKey("UserId")]
        [JsonIgnore]
        public virtual User? user { get; set; } = null;


    }

}