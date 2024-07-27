using Microsoft.AspNetCore.Identity;

namespace SmartFin.Entities
{

    public class User : IdentityUser<int>
    {

        public string Name { get; set; }

        public string RefreshToken { get; set; } = string.Empty;
        
        public virtual ICollection<Expense> Expenses { get; } = new List<Expense>();
        
        public virtual ICollection<Goal> Goals { get; } = new List<Goal>();


    }
}