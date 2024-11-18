using Microsoft.EntityFrameworkCore;
using Smartfin.Extensions;
using SmartFin.DbContexts;
using SmartFin.DTOs.Transaction;
using SmartFin.Entities;

namespace SmartFin.Services
{
    public class TransactionService(SmartFinDbContext context, NotificationService notificationService, UserService userService)
    {

        private readonly SmartFinDbContext _context = context;

        private readonly NotificationService _notificationService = notificationService;
        private readonly UserService _userService = userService;

        //private readonly GoalService _goalService = goalService;

        public async Task<List<Transaction>> GetUsersTransactions(int userid)
        {
            return await _context.Transactions.Where(x => x.UserId == userid).ToListAsync();
        }

        public async Task CreateUserTransaction(CreateTransactionDto expenseDto)
        {
            var Transaction = new Transaction
            {
                sum = expenseDto.sum,
                Date = expenseDto.Date.ToUniversalTime(),
                Name = expenseDto.Name,
                UserId = expenseDto.UserId,
                CategoryId = expenseDto.CategoryId
            };
            _context.Transactions.Add(Transaction);
            //await CheckExpenseLimitAndNotify(expenseDto.UserId);
            //await CheckGoalPaymentsPossibility(expenseDto.UserId);
            await _context.SaveChangesAsync();
        }
        public async Task<Transaction> GetTransactionById(int id)
        {
            return await _context.Transactions.FirstOrDefaultAsync(x => x.id == id);
        }

        public async Task<bool> DeleteTransaction(int id)
        {
            var expense = await _context.Transactions.FirstOrDefaultAsync(x => x.id == id);
            if (expense == null)
            {
                return false;
            }
            _context.Transactions.Remove(expense);
            await _context.SaveChangesAsync();
            //await CheckExpenseLimitAndNotify(expense.UserId);

            return true;

        }

        public async Task<bool> UpdateTransaction(int id, UpdateTransactionDto expenseDto)
        {
            var expense = await _context.Transactions.FirstOrDefaultAsync(x => x.id == id);
            if (expense == null)
            {
                return false;
            }
            expense.sum = expenseDto.sum;
            expense.Date = expenseDto.Date.ToUniversalTime();
            expense.Name = expenseDto.Name;

            await context.SaveChangesAsync();
            //await CheckExpenseLimitAndNotify(expense.UserId);
            //await CheckGoalPaymentsPossibility(expense.UserId);
            return true;
        }

        // private async Task CheckGoalPaymentsPossibility(int userId)
        // {
        //     var currentMonth = DateTime.UtcNow.Month;
        //     var currentYear = DateTime.UtcNow.Year;

        //     var monthlyExpenses = await _context.Transactions
        //         .Where(t => t.UserId == userId && t.Date.Month == currentMonth && t.Date.Year == currentYear)
        //         .SumAsync(t => t.sum);

        //     var user = await _context.Users.FindAsync(userId);
        //     if (user == null)
        //         throw new Exception("Пользователь не найден");

        //     var availableFunds = user.MonthlyIncome - monthlyExpenses;
        //     var totalGoalPayments = await _goalService.GetTotalMonthlyGoalPayments(userId);

        //     if (availableFunds < totalGoalPayments)
        //     {
        //         var shortfall = totalGoalPayments - availableFunds;
        //         var message = $"Внимание! После последней транзакции у вас может не хватить {shortfall:C} для внесения платежей по всем целям в этом месяце.";
        //         await _notificationService.CreateNotificationAsync(userId, message);
        //     }
        // }

        private async Task CheckExpenseLimitAndNotify(int userId)
        {
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;

            var monthlyExpenses = await _context.Transactions
                .Where(t => t.UserId == userId && t.Date.Month == currentMonth && t.Date.Year == currentYear)
                .SumAsync(t => t.sum);

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("Пользователь не найден");

            var expenseLimit = user.ExpenseLimit ?? await _userService.CalculateAutomaticExpenseLimit(userId);

            if (monthlyExpenses > expenseLimit)
            {
                var message = $"Внимание! Ваши расходы за текущий месяц ({monthlyExpenses:C}) превысили установленный лимит ({expenseLimit:C}).";
                await _notificationService.CreateNotificationAsync(userId, message);
            }
        }

        // public async Task CheckGoalPaymentsPossibility(int userId, decimal newTransactionAmount)
        // {
        //     var currentMonth = DateTime.UtcNow.Month;
        //     var currentYear = DateTime.UtcNow.Year;

        //     // Получаем расходы за текущий месяц
        //     var monthlyExpenses = await _context.Transactions
        //         .Where(t => t.UserId == userId && t.Date.Month == currentMonth && t.Date.Year == currentYear)
        //         .SumAsync(t => t.sum);

        //     // Добавляем новую транзакцию
        //     monthlyExpenses += newTransactionAmount;

        //     // Получаем информацию о пользователе
        //     var user = await _context.Users.FindAsync(userId);
        //     if (user == null)
        //         throw new Exception("Пользователь не найден");

        //     // Вычисляем доступные средства
        //     var availableFunds = user.MonthlyIncome - monthlyExpenses;

        //     // Получаем общую сумму необходимых взносов по целям
        //     var totalGoalPayments = await _goalService.GetTotalMonthlyGoalPayments(userId);

        //     if (availableFunds < totalGoalPayments)
        //     {
        //         var shortfall = totalGoalPayments - availableFunds;
        //         var message = $"Внимание! После добавления новой транзакции у вас может не хватить {shortfall:C} для внесения платежей по всем целям в этом месяце.";
        //         await _notificationService.CreateNotificationAsync(userId, message);
        //     }
        // }

    }
}