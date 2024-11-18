namespace SmartFin.DTOs.User
{

    public class UserDTO
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public decimal? ExpenseLimit { get; set; }
        public decimal MonthlyIncome { get; set; }


    }
}