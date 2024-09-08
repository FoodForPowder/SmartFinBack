using System.ComponentModel.DataAnnotations;

namespace SmartFin.DTOs.Expense{
    public class UpdateExpenseDto{
         
        public decimal sum { get; set; }

        public DateTime Date { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
       
    }
}