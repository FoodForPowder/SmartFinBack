using System.ComponentModel.DataAnnotations;

namespace SmartFin.DTOs.Expense{
    public class CreateExpenseDto{
         
        public decimal sum { get; set; }

        public DateTime Date { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public int UserId { get; set; }


        public int CategoryId { get; set; }
       
    }
}