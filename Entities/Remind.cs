using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartFin.Entities
{

    public class Remind
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(255)]

        public string message { get; set; }
        [Required]

        public DateTime date { get; set; }

        public int goalId { get; set; }
        [ForeignKey("goalId")]

        public virtual Goal? Goal { get; set; } = null;

    }
}