using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

namespace SmartFin.Entities
{

    public class User : IdentityUser<int>
    {

        public string Name { get; set; }

        public string RefreshToken { get; set; } = string.Empty;

        public decimal? ExpenseLimit { get; set; }
        public decimal MonthlyIncome { get; set; }

        [JsonIgnore]
        public virtual ICollection<Transaction> Transactions { get; } = new List<Transaction>();
        [JsonIgnore]
        public virtual ICollection<Goal> Goals { get; } = new List<Goal>();
        [JsonIgnore]

        public virtual ICollection<Category> Categories { get; } = new List<Category>();


    }
}