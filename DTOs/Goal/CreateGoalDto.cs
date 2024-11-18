using System.ComponentModel.DataAnnotations;

public class CreateGoalDto
{


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
    [Required]
    public int UserId { get; set; }
}