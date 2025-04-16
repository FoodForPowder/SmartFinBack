using Microsoft.EntityFrameworkCore;
using SmartFin.DbContexts;
using SmartFin.Entities;

namespace SmartFin.Services
{
    public class NotificationService
    {
        private readonly SmartFinDbContext _context;

        public NotificationService(
            SmartFinDbContext context
            )
        {
            _context = context;
        }
        public async Task CreateNotificationAsync(int goalId, string message)
        {
            var notification = new Notification
            {
                goalId = goalId,
                Message = message,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(int userId)
        {
            return await _context.Goals
         .Where(g => g.UserId == userId)
         .SelectMany(g => g.notifications)
         .ToListAsync();
        }



        /// <summary>
        /// Проверяет все активные цели и создает уведомления для пользователей
        /// </summary>
        public async Task CheckAllGoalsAndNotifyUsersAsync()
        {


            // Получаем все активные цели
            var activeGoals = await _context.Goals
                .Where(g => g.status == "В процессе")
                .Include(g => g.user)
                .ToListAsync();



            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            foreach (var goal in activeGoals)
            {
                // Проверяем, был ли сделан взнос в текущем месяце
                var isPaymentMade = goal.lastContributionDate.Month == currentMonth &&
                                   goal.lastContributionDate.Year == currentYear;

                if (isPaymentMade)
                {

                    continue;
                }

                // Если взноса не было, проверяем достаточность средств у пользователя
                var userId = goal.UserId;

                // Получаем текущий баланс пользователя (доход - расходы за текущий месяц)
                var monthlyIncome = goal.user.MonthlyIncome;
                var monthlyExpenses = await _context.Transactions
                    .Where(t => t.UserId == userId &&
                           t.Date.Month == currentMonth &&
                           t.Date.Year == currentYear &&
                           t.sum < 0) // Отрицательные значения - расходы
                    .SumAsync(t => t.sum);

                var currentBalance = monthlyIncome + monthlyExpenses; // monthlyExpenses отрицательное число

                if (currentBalance >= goal.payment)
                {
                    // Средств достаточно, но взнос не сделан
                    await NotifySufficientFundsAsync(goal);
                }
                else
                {
                    // Средств недостаточно
                    await NotifyInsufficientFundsAsync(goal, currentBalance);
                }
            }
        }

        /// <summary>
        /// Создает уведомление о том, что средств достаточно для взноса
        /// </summary>
        private async Task NotifySufficientFundsAsync(Goal goal)
        {
            var message = $"У вас достаточно средств для внесения ежемесячного платежа {goal.payment:C} по цели '{goal.name}'. " +
                         "Не забудьте сделать взнос до конца месяца.";

            await CreateNotificationAsync(goal.id, message);
        }

        /// <summary>
        /// Создает уведомление о том, что средств недостаточно для взноса
        /// </summary>
        private async Task NotifyInsufficientFundsAsync(Goal goal, decimal currentBalance)
        {
            var shortfall = goal.payment - currentBalance;
            var message = $"Внимание! У вас недостаточно средств для внесения ежемесячного платежа по цели '{goal.name}'. " +
                         $"Требуемая сумма: {goal.payment:C}, доступно: {currentBalance:C}. " +
                         $"Не хватает: {shortfall:C}. Возможно, вам потребуется пересмотреть план достижения цели.";

            await CreateNotificationAsync(goal.id, message);
        }
    }
}