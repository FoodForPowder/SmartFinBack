using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SmartFin.Entities
{

    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int goalId { get; set; }

        [ForeignKey("goalId")]
        [JsonIgnore]
        public virtual Goal goal { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public bool IsRead { get; set; } = false;

    }
}