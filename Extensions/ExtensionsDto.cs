using SmartFin.DTOs.Expense;
using SmartFin.DTOs.Goal;
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
                UserId = goal.UserId,

            };
        }
        public static ExpenseDto asDto(this Expense expense)
        {
            return new ExpenseDto
            {
                id = expense.id,
                sum = expense.sum,
                Date = expense.Date,
                Name = expense.Name,
                UserId = expense.UserId,
                CategoryId = expense.CategoryId,
            };
        }
    }
}