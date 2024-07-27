using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SmartFin.Entities
{

    public class Goal
    {
        [Key]
        public int id { get; set; }
        [Required]

        public DateTime dateOfStart { get; set; }
        [Required]
        public DateTime dateOfEnd { get; set; }

        public decimal payment { get; set; }
        [Required]
        [StringLength(50)]
        public string name { get; set; }
        [StringLength(255)]
        public string description { get; set; }

        public decimal plannedSum { get; set; }

        public decimal currentSum { get; set; }

        public string status { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        [JsonIgnore]
        public virtual User? user { get; set; } = null;
        
        public virtual ICollection<Remind> Reminds { get; set; } = new List<Remind>();
    }
}