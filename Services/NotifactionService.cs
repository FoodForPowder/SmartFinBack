using System.ComponentModel;
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
         .Where(g => g.Users.Any(x => x.Id == userId))
         .SelectMany(g => g.Notifications)
         .ToListAsync();
        }

        /// <summary>
        /// Проверяет все активные цели и создает уведомления для пользователей
        /// </summary>
        public async Task CheckAllGoalsAndNotifyUsersAsync()
        {
            // Получаем все активные цели с пользователями
            var activeGoals = await _context.Goals
                .Where(g => g.status == "В процессе")
                .Include(g => g.Users)
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

                // Для каждого пользователя в цели
                foreach (var user in goal.Users)
                {
                    // Получаем текущий баланс пользователя
                    var monthlyIncome = user.MonthlyIncome;
                    var monthlyExpenses = await _context.Transactions
                        .Where(t => t.UserId == user.Id &&
                               t.Date.Month == currentMonth &&
                               t.Date.Year == currentYear &&
                               t.sum < 0)
                        .SumAsync(t => t.sum);

                    var currentBalance = monthlyIncome + monthlyExpenses;

                    // Индивидуальный платеж - общий платеж поделенный на количество участников
                    var individualPayment = goal.payment / goal.Users.Count;

                    if (currentBalance >= individualPayment)
                    {
                        // Средств достаточно, но взнос не сделан
                        await NotifySufficientFundsAsync(goal, individualPayment);
                    }
                    else
                    {
                        // Средств недостаточно
                        await NotifyInsufficientFundsAsync(goal, currentBalance, individualPayment);
                    }
                }
            }
        }

        /// <summary>
        /// Создает уведомление о том, что средств достаточно для взноса
        /// </summary>
        private async Task NotifySufficientFundsAsync(Goal goal, decimal individualPayment)
        {
            var message = $"У вас достаточно средств для внесения вашей части ежемесячного платежа {individualPayment:C} " +
                         $"по общей цели '{goal.name}'. Не забудьте сделать взнос до конца месяца.";

            await CreateNotificationAsync(goal.id, message);
        }

        /// <summary>
        /// Создает уведомление о том, что средств недостаточно для взноса
        /// </summary>
        private async Task NotifyInsufficientFundsAsync(Goal goal, decimal currentBalance, decimal individualPayment)
        {
            var shortfall = individualPayment - currentBalance;
            var message = $"Внимание! У вас недостаточно средств для внесения вашей части платежа по общей цели '{goal.name}'. " +
                         $"Ваша часть платежа: {individualPayment:C}, доступно: {currentBalance:C}. " +
                         $"Не хватает: {shortfall:C}. Возможно, вам потребуется пересмотреть план достижения цели.";

            await CreateNotificationAsync(goal.id, message);
        }
    }
}