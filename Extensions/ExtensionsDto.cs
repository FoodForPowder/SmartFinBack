using SmartFin.DTOs.Category;
using SmartFin.DTOs.Expense;
using SmartFin.DTOs.Goal;
using SmartFin.DTOs.User;
using SmartFin.Entities;

namespace Smartfin.Extensions
{

    public static class ExtensionsDto
    {
        public static GoalDto AsDto(this Goal goal)
        {
            return new GoalDto
            {
                id = goal.id,
                dateOfStart = goal.dateOfStart,
                dateOfEnd = goal.dateOfEnd,
                payment = goal.payment,
                name = goal.name,
                description = goal.description,
                plannedSum = goal.plannedSum,
                currentSum = goal.currentSum,
                status = goal.status,
                UserId = goal.Users.Select(u => u.Id).ToList(),
                lastContributionDate = goal.lastContributionDate,
                lastMonthContribution = goal.lastMonthContributionAmount

            };
        }
        public static TransactionDto asDto(this Transaction transaction)
        {
            return new TransactionDto
            {
                id = transaction.id,
                sum = transaction.sum,
                Date = transaction.Date,
                Name = transaction.Name,
                UserId = transaction.UserId,
                CategoryId = transaction.CategoryId,
            };
        }

        public static UserDTO asDto(this User user)
        {
            return new UserDTO
            {
                Name = user.Name,
                ExpenseLimit = user.ExpenseLimit,
                MonthlyIncome = user.MonthlyIncome,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Id = user.Id,
            };
        }

    }
}