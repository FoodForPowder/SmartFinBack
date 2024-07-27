using System.ComponentModel.DataAnnotations;

namespace SmartFin.Entities
{
    public class Category
    {
        [Key]
        public int id { get; set; }
        [Required]
        [StringLength(50)]
        public string name { get; set; }

        public decimal planSum { get; set; }

        public decimal factSum { get; set; }

        public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();


    }

}