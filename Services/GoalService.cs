using Microsoft.EntityFrameworkCore;
using Smartfin.Extensions;
using SmartFin.DbContexts;
using SmartFin.DTOs.Goal;
using SmartFin.DTOs.Transaction;
using SmartFin.Entities;

namespace SmartFin.Services
{
    ///TO DO
    ///Сделать DTO для всего
    public class GoalService(SmartFinDbContext context, NotificationService notificationService, UserService userService, TransactionService transactionService)
    {

        private readonly SmartFinDbContext _context = context;
        private readonly NotificationService _notificationService = notificationService;

        private readonly UserService _userService = userService;

        private readonly TransactionService _transactionService = transactionService;

        public async Task<IEnumerable<Goal>> GetUserGoalsAsync(int userId)
        {
            var goals = await _context.Goals.Where(x => x.UserId == userId).ToListAsync();

            return goals;
        }

        public async Task<(bool isSuccess, string message)> CreateGoalAsync(CreateGoalDto goalDto)
        {
            try
            {
                var monthlyPayment = CalculateMonthlyPayment(goalDto.plannedSum, goalDto.dateOfStart, goalDto.dateOfEnd);


                if (!await CanUserAffordGoal(goalDto.UserId, monthlyPayment))
                {
                    return (true, "Невозможно создать цель. Сумма ежемесячных отчислений превышает 20% дохода пользователя.");
                }

                var newGoal = new Goal
                {
                    dateOfStart = goalDto.dateOfStart.ToUniversalTime(),
                    dateOfEnd = goalDto.dateOfEnd.ToUniversalTime(),
                    payment = monthlyPayment,
                    name = goalDto.name,
                    description = goalDto.description,
                    plannedSum = goalDto.plannedSum,
                    status = "В процессе",
                    UserId = goalDto.UserId,
                };
                await _context.Goals.AddAsync(newGoal);
                await _context.SaveChangesAsync();
                return (true, "Цель успешно создана");
            }
            catch (Exception ex)
            {
                return (false, ex.InnerException.Message);
            }
        }

        public async Task<(bool isAchievable, Goal recalculatedGoal)> RecalculateGoal(GoalDto goalDto)
        {
            var newGoal = new Goal
            {
                dateOfStart = goalDto.dateOfStart,
                dateOfEnd = goalDto.dateOfEnd,
                name = goalDto.name,
                description = goalDto.description,
                plannedSum = goalDto.plannedSum,
                currentSum = goalDto.currentSum,
                status = goalDto.status,
                UserId = goalDto.UserId,
                payment = goalDto.payment
            };

            const int maxMonthsToAdd = 120; // Максимальное количество месяцев для добавления
            for (int i = 1; i <= maxMonthsToAdd; i++)
            {
                newGoal.dateOfEnd = newGoal.dateOfEnd.AddMonths(1);
                newGoal.payment = CalculateMonthlyPayment(newGoal.plannedSum - newGoal.currentSum, DateTime.Now, newGoal.dateOfEnd);

                if (await CanUserAffordGoal(newGoal.UserId, newGoal.payment))
                {
                    return (true, newGoal);
                }

            }
            return (false, null);
        }

        private async Task CalculateGoalPeriod(int userId)
        {
            var user = _userService.GetUserById(userId);
            if (user != null)
            {

            }
        }

        public async Task<Goal> GetGoalByIdAsync(int id)
        {
            var goal = await SearchGoalByIdAsync(id);
            return goal;
        }
        public async Task<bool> UpdateGoalAsync(Goal updateGoal)
        {
            var goal = await GetGoalByIdAsync(updateGoal.id);
            _context.Entry(goal).CurrentValues.SetValues(updateGoal);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> DeleteGoalAsync(int id, int userId)
        {
            var goalToDelete = await SearchGoalByIdAsync(id);
            if (goalToDelete == null)
            {
                return false;
            }
            else
            {
                _context.Goals.Remove(goalToDelete);
                await _context.SaveChangesAsync();
                return true;
            }
        }

        private async Task<Goal> SearchGoalByIdAsync(int id) => await _context.Goals.FindAsync(id);


        /// <summary>
        /// Получить суммарный месячный платеж у не оплаченных целей.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<decimal> GetTotalMonthlyGoalPayments(int userId)
        {
            var activeGoals = await _context.Goals
                .Where(g => g.UserId == userId && g.status == "В процессе")
                .ToListAsync();

            return activeGoals.Sum(g => g.payment);
        }

        private async Task NotifyInsufficientContributionAsync(Goal goal, decimal expected, decimal actual)
        {
            var message = $"Внимание! Вы не смогли отложить необходимую сумму для цели '{goal.name}'. " +
                          $"Ожидалось: {expected:C}, Фактически: {actual:C}. Цель была пересчитана.";
            await _notificationService.CreateNotificationAsync(goal.id, message);
        }



        public async Task ContributeToGoalAsync(int goalId, decimal amount)
        {
            var goal = await GetGoalByIdAsync(goalId);
            if (goal == null)
                return;
            goal.lastMonthContributionAmount = amount;
            goal.lastContributionDate = DateTime.Now.ToUniversalTime();
            goal.currentSum += amount;

            if (goal.payment != amount)
            {
                goal.payment = CalculateMonthlyPayment(goal.plannedSum - goal.currentSum, goal.dateOfStart, goal.dateOfEnd);
            }
            CreateTransactionDto contributeTransaction = new()
            {
                Name = $"Пополнение цели: {goal.name}",
                sum = amount,
                Date = DateTime.Now.ToUniversalTime(),
                UserId = goal.UserId,
                CategoryId = null
            };

            await _transactionService.CreateUserTransaction(contributeTransaction);
            await _context.SaveChangesAsync();

            //await CheckGoalProgressAsync(goalId);
        }
        private decimal CalculateMonthlyPayment(decimal remainingSum, DateTime startDate, DateTime endDate)
        {
            int totalMonths = ((endDate.Year - startDate.Year) * 12) + endDate.Month - startDate.Month;
            return remainingSum / totalMonths;
        }
        private async Task<bool> CanUserAffordGoal(int userId, decimal newMonthlyPayment)
        {
            var user = await _userService.GetUserById(userId);
            if (user == null)
            {
                return false;
            }

            var currentGoals = await _context.Goals
                .Where(g => g.UserId == userId && g.status == "В процессе")
                .ToListAsync();

            decimal totalMonthlyPayments = currentGoals.Sum(g => g.payment) + newMonthlyPayment;

            return totalMonthlyPayments <= (user.MonthlyIncome * 0.2m);
        }
    }
}